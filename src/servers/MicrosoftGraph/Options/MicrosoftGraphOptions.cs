using System.ComponentModel.DataAnnotations;

namespace McpServerDotnet.Servers.MicrosoftGraph.Options;

/// <summary>
/// Strongly-typed configuration for the Microsoft Graph MCP server.
/// Bind from the <c>MicrosoftGraph</c> configuration section.
/// </summary>
public sealed class MicrosoftGraphOptions
{
    /// <summary>The configuration section name used to bind these options.</summary>
    public const string SectionName = "MicrosoftGraph";

    /// <summary>
    /// Gets or sets the Azure AD tenant ID. Required for app-only authentication.
    /// </summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure AD application (client) ID.
    /// Required for app-only (client credentials) authentication.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for client-credentials flow.
    /// Use a certificate or Managed Identity in production instead.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the UPN or Object ID of the user whose data is accessed
    /// when using delegated permission flows. Defaults to <c>"me"</c> for the
    /// signed-in user.
    /// </summary>
    public string UserId { get; set; } = "me";

    /// <summary>
    /// Gets or sets the maximum number of items returned by list operations.
    /// Defaults to 25.
    /// </summary>
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Gets or sets the Graph API version to use. Defaults to <c>v1.0</c>.
    /// </summary>
    public string ApiVersion { get; set; } = "v1.0";
}
