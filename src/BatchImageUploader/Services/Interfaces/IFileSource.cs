using BatchImageUploader.Models;

namespace BatchImageUploader.Services.Interfaces;

public interface IFileSource
{
    Task<UploadItem[]> GetFilesAsync(
        string folderPath,
        SortStrategy strategy,
        string[] allowedExtensions,
        CancellationToken cancellationToken);
}
