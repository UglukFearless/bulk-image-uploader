# Development Rules

## General Rules

1. **Code Language**: All text inside code must be in English.
   - Code comments — in English
   - Variable, method, and class names — in English
   - String literals in code — in English
   - Error messages in code — in English

2. **Architecture**: 
   - Async-first approach: use pure `async/await` pipeline
   - Do not use `Task.Run` for artificial parallelism
   - Separation of concerns through interfaces

3. **Error Handling**:
   - Failed uploads are logged but do not abort the entire batch
   - Support `CancellationToken` for all asynchronous operations

4. **Security**:
   - Validate `TargetDiskFolder` at application startup
   - Prohibit uploading files to Yandex.Disk root
   - Check existence and non-emptiness of `SourceFolder`

5. **Performance**:
   - Use a single `HttpClient` instance for all requests
   - Stream files without full buffering
   - Control parallelism via `SemaphoreSlim`

6. **Technical Constraints**:
   - Minimum .NET version: 8.0
   - Use only `System.Net.Http` (no third-party HTTP libraries)
   - Follow official Yandex.Disk REST API documentation
