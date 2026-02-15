namespace BatchImageUploader.Utils;

public static class RetryHelper
{
    private static readonly Random Random = new();

    public static async Task RetryAsync(
        Func<CancellationToken, Task> operation,
        int maxRetries = 3,
        int minDelayMs = 500,
        int maxDelayMs = 2000,
        CancellationToken cancellationToken = default)
    {
        var lastException = default(Exception);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await operation(cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    var delay = Random.Next(minDelayMs, maxDelayMs + 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        throw lastException!;
    }
}
