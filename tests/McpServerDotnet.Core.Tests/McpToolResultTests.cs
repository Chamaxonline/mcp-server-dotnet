using System.Text.Json;
using FluentAssertions;
using McpServerDotnet.Core;
using Xunit;

namespace McpServerDotnet.Core.Tests;

public sealed class McpToolResultTests
{
    [Fact]
    public void Success_SetsIsSuccessTrue()
    {
        var result = McpToolResult.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.ErrorMessage.Should().BeNull();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void Failure_SetsIsSuccessFalse()
    {
        var result = McpToolResult.Failure<string>("something went wrong", "InternalError");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.ErrorMessage.Should().Be("something went wrong");
        result.ErrorCode.Should().Be("InternalError");
    }

    [Fact]
    public void FromException_UsesExceptionMessageAndTypeName()
    {
        var ex = new InvalidOperationException("oops");

        var result = McpToolResult.FromException<int>(ex);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("oops");
        result.ErrorCode.Should().Be(nameof(InvalidOperationException));
    }

    [Fact]
    public void ToJson_SerializesSuccessResult()
    {
        var result = McpToolResult.Success(42);
        var json = result.ToJson();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").GetInt32().Should().Be(42);
        doc.RootElement.TryGetProperty("error", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_SerializesFailureResult()
    {
        // Use a reference type (object) — the real-world usage in all tools.
        // For unconstrained T, T? compiles to T at runtime for value types,
        // so default(int) is 0 (not null) and WhenWritingNull won't omit it.
        var result = McpToolResult.Failure<object>("not found", "NotFound");
        var json = result.ToJson();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("error").GetString().Should().Be("not found");
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("NotFound");
        doc.RootElement.TryGetProperty("data", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_UsesCamelCase()
    {
        var result = McpToolResult.Success(new { MyProperty = "value" });
        var json = result.ToJson();

        json.Should().Contain("\"myProperty\"");
        json.Should().NotContain("\"MyProperty\"");
    }

    [Fact]
    public void Success_WithComplexType_RoundTrips()
    {
        var payload = new { Id = 1, Name = "Test", Tags = new[] { "a", "b" } };
        var result = McpToolResult.Success(payload);
        var json = result.ToJson();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("data").GetProperty("name").GetString().Should().Be("Test");
    }
}
