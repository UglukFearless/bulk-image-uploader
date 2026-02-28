using System.Threading;
using System.Threading.Tasks;
using BatchImageUploader.Models;

namespace BatchImageUploader.Services.Interfaces;

public interface IWidgetGenerator
{
    /// <summary>
    /// Build (or skip) the 360° widget.
    /// </summary>
    /// <param name="results">
    /// Array of upload results that will be injected into the
    /// template as a JavaScript array.
    /// </param>
    /// <remarks>
    /// The caller is expected to check the settings flag and only
    /// invoke this method when widget generation is enabled.
    /// </remarks>
    /// <param name="cancellationToken">Propagation token.</param>
    /// <param name="templateFilePath">
    /// Optional path to the HTML template.  If <c>null</c> the
    /// implementation can resolve a default (e.g. the copy that
    /// is copied to the output directory by the csproj).
    /// </param>
    /// <returns>
    /// Full path of the generated widget file, or <c>null</c> when
    /// generation was disabled/skipped.
    /// </returns>
    Task<string?> GenerateAsync(
        UploadResult[] results,
        CancellationToken cancellationToken,
        string? templateFilePath = null);
}
