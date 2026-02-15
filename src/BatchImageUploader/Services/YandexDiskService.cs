using System.Net.Http.Headers;
using System.Text.Json;
using BatchImageUploader.Services.Interfaces;

namespace BatchImageUploader.Services;

public class YandexDiskService : ICloudStorageService
{
    private const string BaseUrl = "https://cloud-api.yandex.net/v1/disk";
    private readonly HttpClient _httpClient;
    private readonly string _oauthToken;

    public YandexDiskService(HttpClient httpClient, string oauthToken)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _oauthToken = oauthToken ?? throw new ArgumentNullException(nameof(oauthToken));

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", _oauthToken);
    }

    public async Task EnsureFolderExistsAsync(string remotePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            throw new ArgumentException("Remote path cannot be empty", nameof(remotePath));
        }

        var pathParts = SplitPath(remotePath);
        var currentPath = string.Empty;

        foreach (var part in pathParts)
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                currentPath = part;
            }
            else
            {
                currentPath = $"{currentPath}/{part}";
            }

            await CreateFolderIfNotExistsAsync(currentPath, cancellationToken);
        }
    }

    private static string[] SplitPath(string path)
    {
        return path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private async Task CreateFolderIfNotExistsAsync(string folderPath, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/resources?path={Uri.EscapeDataString(folderPath)}";
        var response = await _httpClient.PutAsync(url, null, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = JsonDocument.Parse(errorContent);
            
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                var errorCode = errorElement.GetString();
                if (errorCode == "DiskPathPointsToExistentDirectoryError")
                {
                    return;
                }
            }
        }

        var fullErrorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Failed to create folder '{folderPath}'. Status: {response.StatusCode}, Response: {fullErrorContent}");
    }

    public async Task<Uri> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            throw new ArgumentException("Local path cannot be empty", nameof(localPath));
        }

        if (string.IsNullOrWhiteSpace(remotePath))
        {
            throw new ArgumentException("Remote path cannot be empty", nameof(remotePath));
        }

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Local file not found: {localPath}");
        }

        var uploadUrl = await GetUploadUrlAsync(remotePath, cancellationToken);
        await UploadFileToUrlAsync(localPath, uploadUrl, cancellationToken);

        var resourceUrl = $"{BaseUrl}/resources?path={Uri.EscapeDataString(remotePath)}";
        return new Uri(resourceUrl);
    }

    public async Task<Uri> PublishResourceAsync(string remotePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            throw new ArgumentException("Remote path cannot be empty", nameof(remotePath));
        }

        var url = $"{BaseUrl}/resources/publish?path={Uri.EscapeDataString(remotePath)}";
        var response = await _httpClient.PutAsync(url, null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Failed to publish resource. Status: {response.StatusCode}, Response: {errorContent}");
        }

        var downloadUrl = await GetDirectDownloadUrlAsync(remotePath, cancellationToken);
        return downloadUrl;
    }

    private async Task<Uri> GetUploadUrlAsync(string remotePath, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/resources/upload?path={Uri.EscapeDataString(remotePath)}&overwrite=true";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorDocument = JsonDocument.Parse(errorContent);
            
            if (errorDocument.RootElement.TryGetProperty("error", out var errorElement))
            {
                var errorCode = errorElement.GetString();
                if (errorCode == "DiskResourceAlreadyExistsError")
                {
                    throw new InvalidOperationException("File already exists");
                }
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Failed to get upload URL. Status: {response.StatusCode}, Response: {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = JsonDocument.Parse(json);
        var href = document.RootElement.GetProperty("href").GetString();

        if (string.IsNullOrWhiteSpace(href))
        {
            throw new InvalidOperationException("Upload URL is empty in response");
        }

        return new Uri(href);
    }

    private async Task UploadFileToUrlAsync(string localPath, Uri uploadUrl, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await _httpClient.PutAsync(uploadUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Failed to upload file. Status: {response.StatusCode}, Response: {errorContent}");
        }
    }

    private async Task<Uri> GetDirectDownloadUrlAsync(string remotePath, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/resources/download?path={Uri.EscapeDataString(remotePath)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Failed to get download URL. Status: {response.StatusCode}, Response: {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var document = JsonDocument.Parse(json);
        var href = document.RootElement.GetProperty("href").GetString();

        if (string.IsNullOrWhiteSpace(href))
        {
            throw new InvalidOperationException("Download URL is empty in response");
        }

        return new Uri(href);
    }
}
