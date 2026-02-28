using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Prevent escaping of HTML-sensitive characters (like '&') so URLs remain readable
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(successfulResults, options);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }
}
