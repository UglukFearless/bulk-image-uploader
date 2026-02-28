using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BatchImageUploader.Models;
using BatchImageUploader.Services.Interfaces;

namespace BatchImageUploader.Services;

public class WidgetGenerator : IWidgetGenerator
{
    private readonly string DefaultTemplateRelative = "360Widget" + Path.DirectorySeparatorChar + "widget_template.html";

    public async Task<string?> GenerateAsync(UploadResult[] results, CancellationToken cancellationToken, string? templateFilePath = null)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        // Resolve template path: prefer provided path, otherwise use the copy in the application base directory
        string templatePath = templateFilePath ?? Path.Combine(AppContext.BaseDirectory, DefaultTemplateRelative);

        if (!File.Exists(templatePath))
        {
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "360Widget", "widget_template.html");
            if (File.Exists(fallback))
            {
                templatePath = fallback;
            }
            else
            {
                throw new FileNotFoundException("Widget template not found.", templatePath);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        string template;
        using (var stream = File.OpenRead(templatePath))
        using (var reader = new StreamReader(stream))
        {
            template = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Serialize results to JSON suitable for embedding
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        string jsonArray = JsonSerializer.Serialize(results, options);

        // Replace marker — must exist. also remove the trailing empty array if present
        const string markerWithArray = "/* DATA_ARRAY */ []";
        const string marker = "/* DATA_ARRAY */";

        string outputHtml;

        if (template.Contains(markerWithArray))
        {
            outputHtml = template.Replace(markerWithArray, jsonArray);
        }
        else if (template.Contains(marker))
        {
            outputHtml = template.Replace(marker, jsonArray);
        }
        else
        {
            throw new InvalidOperationException($"Template '{templatePath}' does not contain required marker '/* DATA_ARRAY */'.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Determine output path: attempt to save next to result.json in cwd if exists else to cwd/widget.html
        string outputDir = Directory.GetCurrentDirectory();
        string resultJsonPath = Path.Combine(outputDir, "result.json");

        string outputPath;
        if (File.Exists(resultJsonPath))
        {
            // place widget next to result.json
            outputPath = Path.Combine(Path.GetDirectoryName(resultJsonPath) ?? outputDir, "widget.html");
        }
        else
        {
            outputPath = Path.Combine(outputDir, "widget.html");
        }

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? outputDir);

        await File.WriteAllTextAsync(outputPath, outputHtml, cancellationToken).ConfigureAwait(false);

        return outputPath;
    }
}
