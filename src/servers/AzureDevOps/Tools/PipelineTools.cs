using System.ComponentModel;
using System.Text.Json;
using McpServerDotnet.Core;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.AzureDevOps.Options;
using McpServerDotnet.Servers.AzureDevOps.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpServerDotnet.Servers.AzureDevOps.Tools;

/// <summary>
/// MCP tools for Azure DevOps Pipelines — listing, querying runs, and triggering builds.
/// </summary>
[McpServerToolType]
public sealed class PipelineTools
{
    private readonly AzureDevOpsHttpClient _client;
    private readonly AzureDevOpsOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<PipelineTools> _logger;

    /// <summary>Initializes a new instance of <see cref="PipelineTools"/>.</summary>
    public PipelineTools(
        AzureDevOpsHttpClient client,
        IOptions<AzureDevOpsOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<PipelineTools> logger)
    {
        _client = client;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Lists all pipelines defined in the project.
    /// </summary>
    [McpServerTool(Name = "list_pipelines")]
    [Description("Lists all Azure DevOps pipelines (YAML and classic) defined in the project. Returns pipeline ID, name, folder, and revision.")]
    public async Task<string> ListPipelinesAsync(
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        [Description("Maximum number of pipelines to return. Defaults to 100.")] int top = 100,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListPipelinesAsync),
            async () =>
            {
                var proj = project ?? _options.Project;
                var url = _client.BuildUrl($"{proj}/_apis/pipelines", $"$top={top}");
                _logger.LogInformation("Listing pipelines in project {Project}", proj);
                var json = await _client.GetStringAsync(url, cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }

    /// <summary>
    /// Retrieves the details and result of a specific pipeline run.
    /// </summary>
    [McpServerTool(Name = "get_pipeline_run")]
    [Description("Retrieves the details of a specific Azure DevOps pipeline run, including state, result, timing, and triggered parameters.")]
    public async Task<string> GetPipelineRunAsync(
        [Description("The numeric pipeline definition ID")] int pipelineId,
        [Description("The numeric run ID to retrieve")] int runId,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(GetPipelineRunAsync),
            async () =>
            {
                var proj = project ?? _options.Project;
                var url = _client.BuildUrl($"{proj}/_apis/pipelines/{pipelineId}/runs/{runId}");
                _logger.LogInformation(
                    "Fetching run {RunId} for pipeline {PipelineId} in {Project}", runId, pipelineId, proj);
                var json = await _client.GetStringAsync(url, cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }

    /// <summary>
    /// Triggers a new run of an Azure DevOps pipeline.
    /// </summary>
    [McpServerTool(Name = "trigger_pipeline")]
    [Description("Triggers a new run of an Azure DevOps pipeline. Optionally overrides the branch, commit, or runtime variables.")]
    public async Task<string> TriggerPipelineAsync(
        [Description("The numeric pipeline definition ID")] int pipelineId,
        [Description("The branch to build (e.g. 'main' or 'refs/heads/feature/x'). Defaults to the pipeline's default branch.")] string? branch = null,
        [Description("A JSON object of runtime variable overrides, e.g. {\"myVar\": \"value\"}. Pass null for no overrides.")] string? variablesJson = null,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(TriggerPipelineAsync),
            async () =>
            {
                var proj = project ?? _options.Project;

                var resources = branch is null
                    ? null
                    : (object)new
                    {
                        repositories = new
                        {
                            self = new { refName = branch.StartsWith("refs/", StringComparison.OrdinalIgnoreCase)
                                ? branch : $"refs/heads/{branch}" }
                        }
                    };

                object? variables = null;
                if (variablesJson is not null)
                {
                    variables = JsonSerializer.Deserialize<Dictionary<string, object>>(variablesJson);
                }

                var payload = new { resources, variables };
                var url = _client.BuildUrl($"{proj}/_apis/pipelines/{pipelineId}/runs");
                var body = JsonSerializer.Serialize(payload);
                _logger.LogInformation("Triggering pipeline {PipelineId} in {Project}", pipelineId, proj);
                var response = await _client.PostJsonAsync(url, body, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }
}
