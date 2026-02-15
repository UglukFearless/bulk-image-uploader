using System.Text.Json;
using BatchImageUploader.Models;

namespace BatchImageUploader.Startup;

public static class SettingsLoader
{
    private const string SettingsFileName = "settings.json";

    public static Settings Load(string? settingsPath = null)
    {
        var path = settingsPath ?? SettingsFileName;

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Settings file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var settings = JsonSerializer.Deserialize<Settings>(json, options);

        if (settings == null)
        {
            throw new InvalidOperationException("Failed to deserialize settings file");
        }

        ValidateSettings(settings);

        return settings;
    }

    private static void ValidateSettings(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SourceFolder))
        {
            throw new ArgumentException("SourceFolder cannot be empty", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(settings.TargetDiskFolder))
        {
            throw new ArgumentException("TargetDiskFolder is mandatory and cannot be empty", nameof(settings));
        }

        var normalizedTarget = settings.TargetDiskFolder.Trim();
        if (normalizedTarget == "/" || normalizedTarget == "." || normalizedTarget == string.Empty)
        {
            throw new ArgumentException(
                "TargetDiskFolder cannot be '/', '.', or whitespace-only",
                nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(settings.OAuthToken))
        {
            throw new ArgumentException("OAuthToken cannot be empty", nameof(settings));
        }

        if (settings.MaxParallelUploads < 1)
        {
            throw new ArgumentException("MaxParallelUploads must be at least 1", nameof(settings));
        }

        if (settings.AllowedExtensions == null || settings.AllowedExtensions.Length == 0)
        {
            throw new ArgumentException("AllowedExtensions cannot be empty", nameof(settings));
        }
    }
}
