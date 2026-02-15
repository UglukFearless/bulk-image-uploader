using System.Net.Http;
using System.IO;
using BatchImageUploader.Services;
using BatchImageUploader.Services.Interfaces;
using BatchImageUploader.Startup;

try
{
    var settings = SettingsLoader.Load();
    var cancellationToken = CancellationToken.None;

    Console.WriteLine("Loading files...");
    var fileSource = new LocalFileSource();
    var files = await fileSource.GetFilesAsync(
        settings.SourceFolder,
        settings.SortStrategy,
        settings.AllowedExtensions,
        cancellationToken);

    Console.WriteLine($"Found {files.Length} files to upload");

    Console.WriteLine("Initializing cloud storage...");
    using var httpClient = new HttpClient();
    var cloudStorage = new YandexDiskService(httpClient, settings.OAuthToken);

    Console.WriteLine("Ensuring target folder exists...");
    await cloudStorage.EnsureFolderExistsAsync(settings.TargetDiskFolder, cancellationToken);

    Console.WriteLine($"Starting upload with {settings.MaxParallelUploads} parallel uploads...");
    var orchestrator = new UploadOrchestrator(cloudStorage, settings.TargetDiskFolder);
    var results = await orchestrator.UploadFilesAsync(
        files,
        settings.MaxParallelUploads,
        cancellationToken);

    Console.WriteLine("\nUpload results:");
    var successCount = 0;
    var failedCount = 0;

    foreach (var result in results)
    {
        if (result != null)
        {
            Console.WriteLine($"  [{result.Index}] {result.Filename} -> {result.PublicUrl}");
            successCount++;
        }
        else
        {
            failedCount++;
        }
    }

    Console.WriteLine($"\nSummary: {successCount} successful, {failedCount} failed");

    Console.WriteLine("Saving results to result.json...");
    await ResultWriter.WriteResultsAsync(results, cancellationToken: cancellationToken);
    Console.WriteLine("Results saved successfully");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}