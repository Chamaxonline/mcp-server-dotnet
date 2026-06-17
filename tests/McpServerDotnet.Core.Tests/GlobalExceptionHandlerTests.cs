using System.Text.Json;
using FluentAssertions;
using McpServerDotnet.Core;
using McpServerDotnet.Core.Middleware;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServerDotnet.Core.Tests;

public sealed class GlobalExceptionHandlerTests
{
    private static GlobalExceptionHandler CreateHandler() =>
        new(NullLogger<GlobalExceptionHandler>.Instance);

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess_WhenOperationSucceeds()
    {
        var handler = CreateHandler();

        var json = await handler.ExecuteAsync<string>(
            "test",
            () => Task.FromResult(McpToolResult.Success("ok")));

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").GetString().Should().Be("ok");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenOperationThrows()
    {
        var handler = CreateHandler();

        var json = await handler.ExecuteAsync<string>(
            "failing",
            () => throw new InvalidOperationException("boom"));

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("error").GetString().Should().Contain("boom");
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCancelledResult_WhenCancelled()
    {
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var json = await handler.ExecuteAsync<string>(
            "cancelled",
            () => Task.FromResult(McpToolResult.Success("never")),
            cts.Token);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("Cancelled");
    }

    [Fact]
    public async Task ExecuteAsync_HandlesHttpRequestException()
    {
        var handler = CreateHandler();

        var json = await handler.ExecuteAsync<string>(
            "http",
            () => throw new HttpRequestException("connection refused", null, System.Net.HttpStatusCode.ServiceUnavailable));

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("ServiceUnavailable");
    }
}
