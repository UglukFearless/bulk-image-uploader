using System.Text.Json;
using BatchImageUploader.Models;

namespace BatchImageUploader.Services;

public static class ResultWriter
{
    private const string ResultFileName = "result.json";

    public static async Task WriteResultsAsync(
        UploadResult[] results,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = outputPath ?? ResultFileName;
        var successfulResults = results
            .Where(r => r != null)
            .OrderBy(r => r.Index)
            .ToArray();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(successfulResults, options);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }
}
