# TODO: Implementation Phases and Tasks

## Phase 1: Core Models and Interfaces

- [ ] Create `Settings` (POCO for `System.Text.Json` deserialization)
- [ ] Create `UploadItem` (`int Index`, `string FilePath`)
- [ ] Create `UploadResult` (`int Index`, `string Filename`, `string PublicUrl`)
- [ ] Create `SortStrategy` enum with values `{ LikeString, LikeNumbers }`
- [ ] Create `IFileSource` interface
- [ ] Create `ICloudStorageService` interface

## Phase 2: File Source and Sorting

- [ ] Implement `LocalFileSource : IFileSource`
- [ ] Add validation for `SourceFolder` existence and non-emptiness
- [ ] Implement file enumeration with filtering by `AllowedExtensions` (case-insensitive)
- [ ] Implement `LikeString` sorting: `files.OrderBy(Path.GetFileName)`
- [ ] Implement `LikeNumbers` sorting: custom comparer with numeric tokenization
- [ ] Return `UploadItem[]` with pre-assigned indices (0..N-1)

## Phase 3: Yandex.Disk Service

- [ ] Implement `YandexDiskService : ICloudStorageService`
- [ ] Set up singleton with injected `HttpClient` and OAuth token
- [ ] Implement `EnsureFolderExistsAsync` — create directory via `PUT /v1/disk/resources?path={folder}`
- [ ] Implement `UploadFileAsync` — obtain direct upload URL and stream file content
- [ ] Implement `PublishResourceAsync` — publish resource and return public URL

## Phase 4: Upload Orchestrator

- [ ] Pre-allocate array `UploadResult[] results = new UploadResult[fileCount]`
- [ ] **Critical**: call `await _cloudStorage.EnsureFolderExistsAsync(targetFolder, ct)` **once before** spawning consumer tasks
- [ ] Initialize `SemaphoreSlim(settings.MaxParallelUploads)`
- [ ] Spawn exactly `MaxParallelUploads` consumer tasks:
  - Use `ConcurrentQueue<UploadItem>` for work distribution
  - In loop `while (_queue.TryDequeue(out var item))`:
    - Acquire semaphore
    - Upload file via `UploadFileAsync`
    - Publish resource via `PublishResourceAsync`
    - Save result to `results[item.Index]`
    - Release semaphore
- [ ] Await all tasks via `Task.WhenAll()`

## Phase 5: Program Entry Point

- [ ] Load `settings.json` from working directory
- [ ] Validate `TargetDiskFolder` (non-empty, not `/`, not `.`, normalized path)
- [ ] Register services (manual composition or minimal DI container)
- [ ] Execute pipeline:
  1. Get sorted files via `IFileSource`
  2. Ensure remote folder exists via `ICloudStorageService.EnsureFolderExistsAsync` (**outside semaphore**)
  3. Run parallel upload orchestrator
- [ ] Serialize successful results to `result.json` (ordered by index)
- [ ] Output to console: real-time progress counter + final statistics (success/failed)

## Additional Tasks

- [ ] Create `settings.example.json` with configuration example
- [ ] Add Ctrl+C handling (cancellation)
- [ ] Implement error logging to console
- [ ] Add OAuth token validation
- [ ] Test all sorting scenarios
- [ ] Test network error handling
- [ ] Test handling of non-existent files
