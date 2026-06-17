using Microsoft.Extensions.Logging;

namespace McpServerDotnet.Core.Middleware;

/// <summary>
/// Provides helper methods for translating exceptions into structured
/// <see cref="McpToolResult{T}"/> JSON payloads within MCP tool implementations.
/// Inject this service to get consistent error handling across all tools.
/// </summary>
public sealed class GlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="GlobalExceptionHandler"/>.</summary>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes <paramref name="operation"/> and catches any exception, returning a
    /// well-formed <see cref="McpToolResult{T}"/> regardless of outcome.
    /// </summary>
    /// <typeparam name="T">The expected result payload type on success.</typeparam>
    /// <param name="operationName">A short label used in log messages.</param>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task<string> ExecuteAsync<T>(
        string operationName,
        Func<Task<McpToolResult<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await operation();
            return result.ToJson();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation {Operation} was cancelled", operationName);
            return McpToolResult.Failure<T>("Operation was cancelled", "Cancelled").ToJson();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during {Operation}: {StatusCode}", operationName, ex.StatusCode);
            return McpToolResult.Failure<T>(
                $"HTTP error: {ex.Message}",
                ex.StatusCode?.ToString() ?? "HttpError").ToJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Authorization failure during {Operation}", operationName);
            return McpToolResult.Failure<T>("Unauthorized: " + ex.Message, "Unauthorized").ToJson();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {Operation}", operationName);
            return McpToolResult.FromException<T>(ex).ToJson();
        }
    }
}
