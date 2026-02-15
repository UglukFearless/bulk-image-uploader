# Batch Image Uploader for Yandex.Disk

A .NET 8 console application for batch uploading image sequences to Yandex.Disk with automatic public link generation. Designed for 360° product visualization workflows where multiple images (typically 36 frames) must be uploaded, sorted, and exported as an ordered JSON array of public URLs.

## Key Features

- Parallel uploads with configurable concurrency limit
- Configurable file sorting strategies including natural numeric ordering
- Mandatory target folder with automatic creation
- Atomic file distribution via `ConcurrentQueue<T>`
- Progress tracking and error resilience
- Clean separation of concerns through interface-based architecture

## Requirements

- .NET 8.0 or higher
- Yandex.Disk OAuth token with `disk:write` scope

## Quick Start

1. Copy `settings.example.json` to `settings.json`
2. Fill in parameters in `settings.json`:
   - `SourceFolder` — local folder with images
   - `TargetDiskFolder` — path on Yandex.Disk (mandatory, cannot be empty, `/`, or `.`)
   - `OAuthToken` — your Yandex.Disk OAuth token
   - `MaxParallelUploads` — number of concurrent uploads (default: 4)
   - `SortStrategy` — sorting strategy: `"LikeString"` or `"LikeNumbers"`
   - `AllowedExtensions` — allowed file extensions

3. Run the application:
   ```bash
   dotnet run
   ```

4. Results will be saved to `result.json`

## Settings Format (settings.json)

```json
{
  "SourceFolder": "C:/photos/sequence_001",
  "TargetDiskFolder": "3d_visualization/product_123",
  "SortStrategy": "LikeNumbers",
  "OAuthToken": "YOUR_YANDEX_DISK_OAUTH_TOKEN",
  "MaxParallelUploads": 4,
  "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".webp" ]
}
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `SourceFolder` | string | Local directory containing images |
| `TargetDiskFolder` | string | **Mandatory**. Remote Yandex.Disk path (e.g., `"projects/3d/product_x"`). Must not be empty, `/`, `.`, or whitespace-only. |
| `SortStrategy` | enum | `"LikeString"` (ordinal string sort) or `"LikeNumbers"` (natural sort: 1, 2, 11 instead of 1, 11, 2) |
| `OAuthToken` | string | Yandex.Disk OAuth 2.0 token with `disk:write` scope |
| `MaxParallelUploads` | int | Concurrent upload limit (default: 4) |
| `AllowedExtensions` | string[] | Whitelist of image extensions |

## Result Format (result.json)

```json
[
  { "index": 0, "filename": "frame_001.jpg", "publicUrl": "https://disk.yandex.ru/i/xxxxx" },
  { "index": 1, "filename": "frame_002.jpg", "publicUrl": "https://disk.yandex.ru/i/yyyyy" },
  ...
]
```

## Sorting Strategies

- **LikeString**: standard ordinal string comparison (`Path.GetFileName(file)`)
- **LikeNumbers**: natural sort extracting numeric sequences and comparing them as integers:
  - Example: `["img_1.jpg", "img_2.jpg", "img_11.jpg"]` instead of `["img_1.jpg", "img_11.jpg", "img_2.jpg"]`
  - Implementation: regex tokenization into text/number chunks with numeric comparison where applicable

## Project Structure

```
BatchImageUploader/
├── Models/
│   ├── Settings.cs
│   ├── UploadItem.cs
│   ├── UploadResult.cs
│   └── SortStrategy.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IFileSource.cs
│   │   └── ICloudStorageService.cs
│   ├── LocalFileSource.cs
│   ├── NaturalSortComparer.cs
│   ├── YandexDiskService.cs
│   └── UploadOrchestrator.cs
├── settings.example.json
├── Program.cs
└── BatchImageUploader.csproj
```

## Technical Details

### Architecture

- **Async-first**: pure `async/await` pipeline without `Task.Run` for artificial parallelism
- **Work distribution**: `ConcurrentQueue<UploadItem>` for atomic file distribution
- **Concurrency control**: `SemaphoreSlim` held during entire upload cycle per file (upload → publish)
- **HTTP client**: single `HttpClient` instance reused across all upload tasks
- **Cancellation**: full `CancellationToken` support (Ctrl+C handling)
- **Error handling**: failed uploads logged to console but do not abort entire batch

### Interfaces

- **IFileSource** — provides files from local filesystem
- **ICloudStorageService** — cloud storage operations (provider-agnostic)
- **YandexDiskService** — concrete implementation of `ICloudStorageService` for Yandex.Disk

Architecture enables easy provider replacement without orchestrator changes.

### Critical Requirements

- **Root pollution prevention**: validate `TargetDiskFolder` at startup. Reject: `null`, `""`, `"/"`, `"."`, whitespace-only
- **Folder creation timing**: `EnsureFolderExistsAsync` called **once before** upload phase, outside semaphore scope
- **Atomic file distribution**: `ConcurrentQueue<UploadItem>` + `TryDequeue()` — zero locks required
- **Result ordering**: pre-allocated array indexed by `UploadItem.Index` — no post-sorting needed
- **Natural numeric sort**: filename tokenization into chunks; numbers compared as `int`, text as `string`
- **Semaphore scope**: acquired before upload start per file, released after publish completes
- **Memory safety**: stream files directly (`FileStream` → `StreamContent`) without full buffering
- **Error isolation**: individual upload failure does not cancel other tasks or entire batch

## Documentation

- [RULES.md](RULES.md) — development rules
- [TODO.md](TODO.md) — implementation phases and tasks
