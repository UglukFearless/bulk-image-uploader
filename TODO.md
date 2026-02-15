# TODO: Implementation Phases and Tasks

## Phase 1: Core Models and Interfaces

- [x] Create `Settings` (POCO for `System.Text.Json` deserialization)
- [x] Create `UploadItem` (`int Index`, `string FilePath`)
- [x] Create `UploadResult` (`int Index`, `string Filename`, `string PublicUrl`)
- [x] Create `SortStrategy` enum with values `{ LikeString, LikeNumbers }`
- [x] Create `IFileSource` interface
- [x] Create `ICloudStorageService` interface

## Phase 2: File Source and Sorting

- [x] Implement `LocalFileSource : IFileSource`
- [x] Add validation for `SourceFolder` existence and non-emptiness
- [x] Implement file enumeration with filtering by `AllowedExtensions` (case-insensitive)
- [x] Implement `LikeString` sorting: `files.OrderBy(Path.GetFileName)`
- [x] Implement `LikeNumbers` sorting: custom comparer with numeric tokenization
- [x] Return `UploadItem[]` with pre-assigned indices (0..N-1)

## Phase 3: Yandex.Disk Service

- [x] Implement `YandexDiskService : ICloudStorageService`
- [x] Set up singleton with injected `HttpClient` and OAuth token
- [x] Implement `EnsureFolderExistsAsync` — create directory via `PUT /v1/disk/resources?path={folder}`
- [x] Implement `UploadFileAsync` — obtain direct upload URL and stream file content
- [x] Implement `PublishResourceAsync` — publish resource and return public URL

## Phase 4: Upload Orchestrator

- [x] Pre-allocate array `UploadResult[] results = new UploadResult[fileCount]`
- [x] **Critical**: call `await _cloudStorage.EnsureFolderExistsAsync(targetFolder, ct)` **once before** spawning consumer tasks
- [x] Initialize `SemaphoreSlim(settings.MaxParallelUploads)`
- [x] Spawn exactly `MaxParallelUploads` consumer tasks:
  - Use `ConcurrentQueue<UploadItem>` for work distribution
  - In loop `while (_queue.TryDequeue(out var item))`:
    - Acquire semaphore
    - Upload file via `UploadFileAsync`
    - Publish resource via `PublishResourceAsync`
    - Save result to `results[item.Index]`
    - Release semaphore
- [x] Await all tasks via `Task.WhenAll()`

## Phase 5: Program Entry Point

- [x] Load `settings.json` from working directory
- [x] Validate `TargetDiskFolder` (non-empty, not `/`, not `.`, normalized path)
- [x] Register services (manual composition or minimal DI container)
- [x] Execute pipeline:
  1. Get sorted files via `IFileSource`
  2. Ensure remote folder exists via `ICloudStorageService.EnsureFolderExistsAsync` (**outside semaphore**)
  3. Run parallel upload orchestrator
- [x] Serialize successful results to `result.json` (ordered by index)
- [x] Output to console: real-time progress counter + final statistics (success/failed)

## Additional Tasks

- [x] Create `settings.example.json` with configuration example
- [ ] Add Ctrl+C handling (cancellation)
- [ ] Implement error logging to console
- [ ] Add OAuth token validation
- [ ] Test all sorting scenarios
- [ ] Test network error handling
- [ ] Test handling of non-existent files
