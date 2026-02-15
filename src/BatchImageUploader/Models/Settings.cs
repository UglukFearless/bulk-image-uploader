using System.Text.Json.Serialization;

namespace BatchImageUploader.Models;

public class Settings
{
    [JsonPropertyName("SourceFolder")]
    public string SourceFolder { get; set; } = string.Empty;

    [JsonPropertyName("TargetDiskFolder")]
    public string TargetDiskFolder { get; set; } = string.Empty;

    [JsonPropertyName("SortStrategy")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortStrategy SortStrategy { get; set; } = SortStrategy.LikeString;

    [JsonPropertyName("OAuthToken")]
    public string OAuthToken { get; set; } = string.Empty;

    [JsonPropertyName("MaxParallelUploads")]
    public int MaxParallelUploads { get; set; } = 4;

    [JsonPropertyName("AllowedExtensions")]
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}
