using System.Collections.Concurrent;
using BatchImageUploader.Models;
using BatchImageUploader.Services.Interfaces;
using BatchImageUploader.Utils;

namespace BatchImageUploader.Services;

public class UploadOrchestrator
{
    private readonly ICloudStorageService _cloudStorage;
    private readonly string _targetFolder;
    private int _completedCount;
    private readonly object _lockObject = new();

    public UploadOrchestrator(ICloudStorageService cloudStorage, string targetFolder)
    {
        _cloudStorage = cloudStorage ?? throw new ArgumentNullException(nameof(cloudStorage));
        _targetFolder = targetFolder ?? throw new ArgumentNullException(nameof(targetFolder));
    }

    public async Task<UploadResult[]> UploadFilesAsync(
        UploadItem[] files,
        int maxParallelUploads,
        CancellationToken cancellationToken)
    {
        var results = new UploadResult[files.Length];
        var queue = new ConcurrentQueue<UploadItem>(files);
        var tasks = new List<Task>();

        _completedCount = 0;

        for (int i = 0; i < maxParallelUploads; i++)
        {
            var task = ProcessUploadQueueAsync(queue, results, files.Length, cancellationToken);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        return results;
    }

    private async Task ProcessUploadQueueAsync(
        ConcurrentQueue<UploadItem> queue,
        UploadResult[] results,
        int totalFiles,
        CancellationToken cancellationToken)
    {
        while (queue.TryDequeue(out var item))
        {
            try
            {
                var remotePath = $"{_targetFolder}/{Path.GetFileName(item.FilePath)}";
                var fileName = Path.GetFileName(item.FilePath);

                await RetryHelper.RetryAsync(
                    async ct =>
                    {
                        try
                        {
                            await _cloudStorage.UploadFileAsync(item.FilePath, remotePath, ct);
                            var downloadUrl = await _cloudStorage.PublishResourceAsync(remotePath, ct);

                            lock (_lockObject)
                            {
                                _completedCount++;
                                Console.WriteLine(
                                    $"[{_completedCount}/{totalFiles}] ✓ {fileName} -> {downloadUrl}");
                            }

                            results[item.Index] = new UploadResult(
                                item.Index,
                                fileName,
                                downloadUrl.ToString());
                        }
                        catch (InvalidOperationException ex) when (ex.Message == "File already exists")
                        {
                            var downloadUrl = await _cloudStorage.PublishResourceAsync(remotePath, ct);

                            lock (_lockObject)
                            {
                                _completedCount++;
                                Console.WriteLine(
                                    $"[{_completedCount}/{totalFiles}] ⊙ {fileName} (already exists) -> {downloadUrl}");
                            }

                            results[item.Index] = new UploadResult(
                                item.Index,
                                fileName,
                                downloadUrl.ToString());
                        }
                    },
                    maxRetries: 3,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                lock (_lockObject)
                {
                    _completedCount++;
                    Console.Error.WriteLine(
                        $"[{_completedCount}/{totalFiles}] ✗ Failed to upload {Path.GetFileName(item.FilePath)}: {ex.Message}");
                }
            }
        }
    }
}
