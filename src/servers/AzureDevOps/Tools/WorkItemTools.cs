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
/// MCP tools for Azure DevOps work item operations (read and write).
/// </summary>
[McpServerToolType]
public sealed class WorkItemTools
{
    private readonly AzureDevOpsHttpClient _client;
    private readonly AzureDevOpsOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<WorkItemTools> _logger;

    /// <summary>Initializes a new instance of <see cref="WorkItemTools"/>.</summary>
    public WorkItemTools(
        AzureDevOpsHttpClient client,
        IOptions<AzureDevOpsOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<WorkItemTools> logger)
    {
        _client = client;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a single Azure DevOps work item by its numeric ID.
    /// Returns the work item fields including title, state, type, assignee, and description.
    /// </summary>
    [McpServerTool(Name = "get_work_item")]
    [Description("Retrieves a single Azure DevOps work item by its numeric ID, including title, state, type, assignee, description, and tags.")]
    public async Task<string> GetWorkItemAsync(
        [Description("The numeric work item ID (e.g. 1234)")] int id,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(GetWorkItemAsync),
            async () =>
            {
                var proj = project ?? _options.Project;
                var url = _client.BuildUrl(
                    $"{proj}/_apis/wit/workitems/{id}",
                    "$expand=all");
                _logger.LogInformation("Fetching work item {Id} from project {Project}", id, proj);
                var json = await _client.GetStringAsync(url, cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }

    /// <summary>
    /// Creates a new Azure DevOps work item.
    /// </summary>
    [McpServerTool(Name = "create_work_item")]
    [Description("Creates a new Azure DevOps work item of the specified type in the given project.")]
    public async Task<string> CreateWorkItemAsync(
        [Description("The work item type, e.g. 'Bug', 'Task', 'User Story', 'Feature'")] string type,
        [Description("The title (System.Title) for the new work item")] string title,
        [Description("An optional description (System.Description)")] string? description = null,
        [Description("The optional assignee display name or email")] string? assignedTo = null,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(CreateWorkItemAsync),
            async () =>
            {
                var proj = project ?? _options.Project;
                var patches = new List<object>
                {
                    new { op = "add", path = "/fields/System.Title", value = title },
                };

                if (description is not null)
                {
                    patches.Add(new { op = "add", path = "/fields/System.Description", value = description });
                }

                if (assignedTo is not null)
                {
                    patches.Add(new { op = "add", path = "/fields/System.AssignedTo", value = assignedTo });
                }

                var url = _client.BuildUrl($"{proj}/_apis/wit/workitems/${Uri.EscapeDataString(type)}");
                var body = JsonSerializer.Serialize(patches);
                _logger.LogInformation("Creating {Type} work item in project {Project}", type, proj);
                var response = await _client.PatchJsonAsync(url, body, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }

    /// <summary>
    /// Updates one or more fields on an existing Azure DevOps work item.
    /// </summary>
    [McpServerTool(Name = "update_work_item")]
    [Description("Updates one or more fields on an existing Azure DevOps work item. Pass only the fields you want to change.")]
    public async Task<string> UpdateWorkItemAsync(
        [Description("The numeric work item ID to update")] int id,
        [Description("New title (System.Title). Omit to leave unchanged.")] string? title = null,
        [Description("New state (System.State), e.g. 'Active', 'Resolved', 'Closed'. Omit to leave unchanged.")] string? state = null,
        [Description("New description (System.Description). Omit to leave unchanged.")] string? description = null,
        [Description("New assignee display name or email. Omit to leave unchanged.")] string? assignedTo = null,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(UpdateWorkItemAsync),
            async () =>
            {
                var proj = project ?? _options.Project;
                var patches = new List<object>();

                if (title is not null)
                {
                    patches.Add(new { op = "replace", path = "/fields/System.Title", value = title });
                }

                if (state is not null)
                {
                    patches.Add(new { op = "replace", path = "/fields/System.State", value = state });
                }

                if (description is not null)
                {
                    patches.Add(new { op = "replace", path = "/fields/System.Description", value = description });
                }

                if (assignedTo is not null)
                {
                    patches.Add(new { op = "replace", path = "/fields/System.AssignedTo", value = assignedTo });
                }

                if (patches.Count == 0)
                {
                    return McpToolResult.Failure<object>("No fields specified to update.", "NoChanges");
                }

                var url = _client.BuildUrl($"{proj}/_apis/wit/workitems/{id}");
                var body = JsonSerializer.Serialize(patches);
                _logger.LogInformation("Updating work item {Id} in project {Project}", id, proj);
                var response = await _client.PatchJsonAsync(url, body, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }
}
