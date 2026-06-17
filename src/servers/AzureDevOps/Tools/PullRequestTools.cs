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
/// MCP tools for Azure DevOps Git pull request operations.
/// </summary>
[McpServerToolType]
public sealed class PullRequestTools
{
    private readonly AzureDevOpsHttpClient _client;
    private readonly AzureDevOpsOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<PullRequestTools> _logger;

    /// <summary>Initializes a new instance of <see cref="PullRequestTools"/>.</summary>
    public PullRequestTools(
        AzureDevOpsHttpClient client,
        IOptions<AzureDevOpsOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<PullRequestTools> logger)
    {
        _client = client;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Lists open pull requests in the specified repository.
    /// </summary>
    [McpServerTool(Name = "list_pull_requests")]
    [Description("Lists pull requests in an Azure DevOps Git repository. Defaults to open PRs. Returns PR ID, title, author, source/target branch, creation date, and status.")]
    public async Task<string> ListPullRequestsAsync(
        [Description("The Git repository name")] string repository,
        [Description("Filter by status: 'active', 'completed', 'abandoned', or 'all'. Defaults to 'active'.")] string status = "active",
        [Description("Maximum number of PRs to return. Defaults to 25.")] int top = 25,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListPullRequestsAsync),
            async () =>
            {
                var proj = project ?? _options.Project;
                var url = _client.BuildUrl(
                    $"{proj}/_apis/git/repositories/{Uri.EscapeDataString(repository)}/pullrequests",
                    $"searchCriteria.status={status}&$top={top}");
                _logger.LogInformation(
                    "Listing {Status} PRs in {Repository}/{Project}", status, repository, proj);
                var json = await _client.GetStringAsync(url, cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }

    /// <summary>
    /// Retrieves detailed information about a specific pull request.
    /// </summary>
    [McpServerTool(Name = "get_pull_request")]
    [Description("Retrieves full details of an Azure DevOps pull request including description, reviewers, commits, and work item links.")]
    public async Task<string> GetPullRequestAsync(
        [Description("The Git repository name")] string repository,
        [Description("The numeric pull request ID")] int pullRequestId,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(GetPullRequestAsync),
            async () =>
            {
                var proj = project ?? _options.Project;
                var url = _client.BuildUrl(
                    $"{proj}/_apis/git/repositories/{Uri.EscapeDataString(repository)}/pullrequests/{pullRequestId}");
                _logger.LogInformation(
                    "Fetching PR {Id} from {Repository}/{Project}", pullRequestId, repository, proj);
                var json = await _client.GetStringAsync(url, cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }

    /// <summary>
    /// Creates a new pull request in an Azure DevOps Git repository.
    /// </summary>
    [McpServerTool(Name = "create_pull_request")]
    [Description("Creates a new Azure DevOps pull request from a source branch into a target branch.")]
    public async Task<string> CreatePullRequestAsync(
        [Description("The Git repository name")] string repository,
        [Description("The source branch name (e.g. 'refs/heads/feature/my-feature' or just 'feature/my-feature')")] string sourceBranch,
        [Description("The target branch name (e.g. 'refs/heads/main' or just 'main')")] string targetBranch,
        [Description("The pull request title")] string title,
        [Description("An optional description for the pull request")] string? description = null,
        [Description("Set to true to mark the PR as a draft")] bool isDraft = false,
        [Description("The Azure DevOps project name. Defaults to the configured default project.")] string? project = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(CreatePullRequestAsync),
            async () =>
            {
                var proj = project ?? _options.Project;

                static string NormalizeBranch(string branch) =>
                    branch.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase)
                        ? branch
                        : $"refs/heads/{branch}";

                var payload = new
                {
                    title,
                    description = description ?? string.Empty,
                    sourceRefName = NormalizeBranch(sourceBranch),
                    targetRefName = NormalizeBranch(targetBranch),
                    isDraft,
                };

                var url = _client.BuildUrl(
                    $"{proj}/_apis/git/repositories/{Uri.EscapeDataString(repository)}/pullrequests");
                var body = JsonSerializer.Serialize(payload);
                _logger.LogInformation(
                    "Creating PR '{Title}' in {Repository}/{Project}", title, repository, proj);
                var response = await _client.PostJsonAsync(url, body, cancellationToken);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(json);
                return McpToolResult.Success<object>(doc.RootElement);
            },
            cancellationToken);
    }
}
