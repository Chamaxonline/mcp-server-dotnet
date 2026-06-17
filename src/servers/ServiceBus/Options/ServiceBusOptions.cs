using System.ComponentModel.DataAnnotations;

namespace McpServerDotnet.Servers.ServiceBus.Options;

/// <summary>
/// Strongly-typed configuration for the Azure Service Bus MCP server.
/// Bind from the <c>ServiceBus</c> configuration section.
/// </summary>
public sealed class ServiceBusOptions
{
    /// <summary>The configuration section name used to bind these options.</summary>
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// Gets or sets the Service Bus namespace host name, e.g.
    /// <c>my-namespace.servicebus.windows.net</c>.
    /// Used when authenticating via <see cref="Azure.Identity.DefaultAzureCredential"/>.
    /// Mutually exclusive with <see cref="ConnectionString"/>.
    /// </summary>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the full connection string for the Service Bus namespace.
    /// When set, takes precedence over <see cref="FullyQualifiedNamespace"/>.
    /// Avoid for production — prefer <see cref="FullyQualifiedNamespace"/> with
    /// Managed Identity instead.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the default queue or topic name. Individual tool calls
    /// may override this by supplying an explicit entity name parameter.
    /// </summary>
    public string DefaultEntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of messages to peek in a single call.
    /// Defaults to 10.
    /// </summary>
    public int DefaultPeekCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum wait time when receiving messages. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(5);
}
