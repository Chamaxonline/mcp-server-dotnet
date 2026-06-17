using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServerDotnet.Core;

/// <summary>
/// A discriminated union that wraps the outcome of an MCP tool invocation.
/// Tools should always return <c>result.ToJson()</c> rather than throwing, so the
/// AI model receives structured error information instead of an unhandled exception.
/// </summary>
/// <typeparam name="T">The type of the successful result payload.</typeparam>
public sealed class McpToolResult<T>
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool IsSuccess { get; init; }

    /// <summary>Gets the result payload when <see cref="IsSuccess"/> is <c>true</c>.</summary>
    [JsonPropertyName("data")]
    public T? Value { get; init; }

    /// <summary>Gets the human-readable error message when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    [JsonPropertyName("error")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a machine-readable error code (e.g. <c>"NotFound"</c>, <c>"Unauthorized"</c>)
    /// when <see cref="IsSuccess"/> is <c>false</c>.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    /// <param name="value">The payload to include in the result.</param>
    public static McpToolResult<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    /// <summary>Creates a failed result with the given <paramref name="error"/> message.</summary>
    /// <param name="error">A human-readable description of what went wrong.</param>
    /// <param name="errorCode">An optional machine-readable code such as an HTTP status name.</param>
    public static McpToolResult<T> Failure(string error, string? errorCode = null) =>
        new() { IsSuccess = false, ErrorMessage = error, ErrorCode = errorCode };

    /// <summary>
    /// Creates a failed result from an exception, using the exception message and type name
    /// as the error description and code respectively.
    /// </summary>
    public static McpToolResult<T> FromException(Exception ex, string? errorCode = null) =>
        Failure(ex.Message, errorCode ?? ex.GetType().Name);

    /// <summary>
    /// Serializes this result to a JSON string suitable for returning directly from
    /// an MCP tool method.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this, s_jsonOptions);
}

/// <summary>
/// Non-generic factory helpers for <see cref="McpToolResult{T}"/>.
/// Prefer these over calling the generic type directly.
/// </summary>
public static class McpToolResult
{
    /// <inheritdoc cref="McpToolResult{T}.Success(T)"/>
    public static McpToolResult<T> Success<T>(T value) => McpToolResult<T>.Success(value);

    /// <inheritdoc cref="McpToolResult{T}.Failure(string, string?)"/>
    public static McpToolResult<T> Failure<T>(string error, string? errorCode = null) =>
        McpToolResult<T>.Failure(error, errorCode);

    /// <inheritdoc cref="McpToolResult{T}.FromException(Exception, string?)"/>
    public static McpToolResult<T> FromException<T>(Exception ex, string? errorCode = null) =>
        McpToolResult<T>.FromException(ex, errorCode);
}
