namespace McpServerDotnet.Core;

/// <summary>
/// Configuration options that control how each MCP server authenticates to Azure and
/// Azure-adjacent services. Supports Managed Identity via <see cref="Azure.Identity.DefaultAzureCredential"/>
/// as well as static API-key / Personal Access Token flows.
/// </summary>
public sealed class AzureAuthOptions
{
    /// <summary>The configuration section name used to bind these options.</summary>
    public const string SectionName = "AzureAuth";

    /// <summary>
    /// Gets or sets the authentication mode.
    /// Defaults to <see cref="AuthMode.ManagedIdentity"/>, which uses
    /// <see cref="Azure.Identity.DefaultAzureCredential"/> and works automatically
    /// with Azure Managed Identity, the Azure CLI, environment variables, and
    /// Visual Studio / VS Code sign-in.
    /// </summary>
    public AuthMode Mode { get; set; } = AuthMode.ManagedIdentity;

    /// <summary>
    /// Gets or sets the static API key or Personal Access Token used when
    /// <see cref="Mode"/> is <see cref="AuthMode.ApiKey"/>.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the Azure AD tenant ID. When set, overrides the tenant resolved
    /// automatically by <see cref="Azure.Identity.DefaultAzureCredential"/>.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client ID for a user-assigned Managed Identity or service
    /// principal. Optional unless multiple managed identities are assigned.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets additional OAuth 2.0 scopes to request alongside the default
    /// scope for a service. Rarely needed; leave empty for most integrations.
    /// </summary>
    public IList<string> AdditionalScopes { get; set; } = [];
}

/// <summary>Selects the credential strategy used to authenticate outbound calls.</summary>
public enum AuthMode
{
    /// <summary>
    /// Use <see cref="Azure.Identity.DefaultAzureCredential"/>, which tries Managed
    /// Identity, Workload Identity, environment variables, the Azure CLI, and IDE
    /// sign-in in order. Recommended for production workloads.
    /// </summary>
    ManagedIdentity,

    /// <summary>
    /// Use a static API key or PAT supplied via <see cref="AzureAuthOptions.ApiKey"/>.
    /// Useful for services that do not support Azure AD (e.g. Azure DevOps PAT).
    /// </summary>
    ApiKey,

    /// <summary>
    /// Force the use of <see cref="Azure.Identity.AzureCliCredential"/>.
    /// Convenient during local development when already signed in via <c>az login</c>.
    /// </summary>
    AzureCli,
}
