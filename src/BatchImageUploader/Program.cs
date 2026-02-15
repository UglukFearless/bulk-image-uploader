using BatchImageUploader.Services;
using BatchImageUploader.Startup;
using System.IO;

try
{
    var settings = SettingsLoader.Load();
    var fileSource = new LocalFileSource();
    var cancellationToken = CancellationToken.None;

    Console.WriteLine("Loading files...");
    var files = await fileSource.GetFilesAsync(
        settings.SourceFolder,
        settings.SortStrategy,
        settings.AllowedExtensions,
        cancellationToken);

    Console.WriteLine($"Found {files.Length} files:");
    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file.FilePath);
        Console.WriteLine($"  [{file.Index}] {fileName}");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}