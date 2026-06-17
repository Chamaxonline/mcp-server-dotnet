using System.Net;
using System.Text.Json;
using FluentAssertions;
using McpServerDotnet.Core;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.AzureDevOps.Options;
using McpServerDotnet.Servers.AzureDevOps.Services;
using McpServerDotnet.Servers.AzureDevOps.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServerDotnet.Servers.AzureDevOps.Tests;

/// <summary>
/// Integration-style skeleton for WorkItemTools.
/// Real tests would use a test Azure DevOps organization or WireMock-style HTTP interception.
/// </summary>
public sealed class WorkItemToolsTests
{
    private static IOptions<AzureDevOpsOptions> CreateOptions(
        string org = "testorg",
        string project = "testproject") =>
        Microsoft.Extensions.Options.Options.Create(new AzureDevOpsOptions
        {
            Organization = org,
            Project = project,
            PersonalAccessToken = "test-pat",
        });

    [Fact]
    public async Task UpdateWorkItemAsync_ReturnsFailure_WhenNoFieldsProvided()
    {
        // Arrange — build a tool instance with a no-op HTTP client stub.
        var httpClient = new HttpClient(new StubHttpMessageHandler(
            HttpStatusCode.OK,
            """{"id": 1, "fields": {}}"""))
        {
            BaseAddress = new Uri("https://dev.azure.com/testorg/")
        };

        var client = new AzureDevOpsHttpClient(httpClient, CreateOptions());
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var tools = new WorkItemTools(
            client,
            CreateOptions(),
            handler,
            NullLogger<WorkItemTools>.Instance);

        // Act
        var json = await tools.UpdateWorkItemAsync(id: 1);

        // Assert
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("NoChanges");
    }

    [Fact]
    public void McpToolResult_Success_IsTrue_ForValidWorkItemResponse()
    {
        var result = McpToolResult.Success(new { id = 42, title = "Fix login bug" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
}

/// <summary>Minimal HTTP message handler stub for unit tests.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _content;

    public StubHttpMessageHandler(HttpStatusCode status, string content)
    {
        _status = status;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(_status)
        {
            Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json"),
        });
}
