using System.Collections.Generic;
using System.IO;
using BatchImageUploader.Models;
using BatchImageUploader.Services.Interfaces;
using BatchImageUploader.Utils;

namespace BatchImageUploader.Services;

public class LocalFileSource : IFileSource
{
    public Task<UploadItem[]> GetFilesAsync(
        string folderPath,
        SortStrategy strategy,
        string[] allowedExtensions,
        CancellationToken cancellationToken)
    {
        ValidateFolder(folderPath);

        var files = Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly)
            .Where(file => IsAllowedExtension(file, allowedExtensions))
            .ToList();

        if (files.Count == 0)
        {
            throw new InvalidOperationException($"No files found in folder: {folderPath}");
        }

        var sortedFiles = SortFiles(files, strategy);
        var uploadItems = sortedFiles
            .Select((file, index) => new UploadItem(index, file))
            .ToArray();

        return Task.FromResult(uploadItems);
    }

    private static void ValidateFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path cannot be empty", nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Source folder does not exist: {folderPath}");
        }
    }

    private static bool IsAllowedExtension(string filePath, string[] allowedExtensions)
    {
        var extension = Path.GetExtension(filePath);
        return allowedExtensions.Any(
            ext => string.Equals(extension, ext, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> SortFiles(IEnumerable<string> files, SortStrategy strategy)
    {
        return strategy switch
        {
            SortStrategy.LikeString => files.OrderBy(Path.GetFileName, StringComparer.Ordinal),
            SortStrategy.LikeNumbers => files.OrderBy(Path.GetFileName, new NaturalSortComparer()),
            _ => throw new ArgumentException($"Unknown sort strategy: {strategy}", nameof(strategy))
        };
    }
}
