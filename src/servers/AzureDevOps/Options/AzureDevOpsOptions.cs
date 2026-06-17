using System.ComponentModel.DataAnnotations;

namespace McpServerDotnet.Servers.AzureDevOps.Options;

/// <summary>
/// Strongly-typed configuration for the Azure DevOps MCP server.
/// Bind from the <c>AzureDevOps</c> configuration section.
/// </summary>
public sealed class AzureDevOpsOptions
{
    /// <summary>The configuration section name used to bind these options.</summary>
    public const string SectionName = "AzureDevOps";

    /// <summary>
    /// Gets or sets the Azure DevOps organization name (the subdomain in
    /// <c>https://dev.azure.com/{Organization}</c>).
    /// </summary>
    [Required]
    public string Organization { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default project name. Individual tool calls may
    /// override this by supplying an explicit project parameter.
    /// </summary>
    [Required]
    public string Project { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Personal Access Token used for Basic authentication.
    /// Leave empty to use <see cref="McpServerDotnet.Core.AzureAuthOptions"/> with
    /// <see cref="Azure.Identity.DefaultAzureCredential"/> instead.
    /// </summary>
    public string? PersonalAccessToken { get; set; }

    /// <summary>
    /// Gets or sets the Azure DevOps REST API version sent in every request.
    /// Defaults to <c>7.2-preview</c>.
    /// </summary>
    public string ApiVersion { get; set; } = "7.2-preview";

    /// <summary>
    /// Gets or sets the HTTP request timeout. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
