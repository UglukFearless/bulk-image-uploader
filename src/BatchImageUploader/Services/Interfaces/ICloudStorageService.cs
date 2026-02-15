namespace BatchImageUploader.Services.Interfaces;

public interface ICloudStorageService
{
    Task EnsureFolderExistsAsync(string remotePath, CancellationToken cancellationToken);
    Task<Uri> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken);
    Task<Uri> PublishResourceAsync(string remotePath, CancellationToken cancellationToken);
}
