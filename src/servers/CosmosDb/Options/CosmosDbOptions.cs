using System.ComponentModel.DataAnnotations;

namespace McpServerDotnet.Servers.CosmosDb.Options;

/// <summary>
/// Strongly-typed configuration for the Azure Cosmos DB MCP server.
/// Bind from the <c>CosmosDb</c> configuration section.
/// </summary>
public sealed class CosmosDbOptions
{
    /// <summary>The configuration section name used to bind these options.</summary>
    public const string SectionName = "CosmosDb";

    /// <summary>
    /// Gets or sets the Cosmos DB account endpoint URI, e.g.
    /// <c>https://my-account.documents.azure.com:443/</c>.
    /// </summary>
    [Required]
    public string AccountEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account key for key-based authentication.
    /// Leave empty to use <see cref="Azure.Identity.DefaultAzureCredential"/>.
    /// </summary>
    public string? AccountKey { get; set; }

    /// <summary>
    /// Gets or sets the default database name used when tools are called without
    /// an explicit database parameter.
    /// </summary>
    public string DefaultDatabase { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default container name used when tools are called without
    /// an explicit container parameter.
    /// </summary>
    public string DefaultContainer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of items returned by a query.
    /// Defaults to 100.
    /// </summary>
    public int MaxItemCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to allow bulk execution mode.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool EnableBulkExecution { get; set; } = false;
}
