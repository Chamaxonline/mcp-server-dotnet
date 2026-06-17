using Azure.Core;
using Azure.Identity;
using McpServerDotnet.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Serilog;
using Serilog.Events;

namespace McpServerDotnet.Core.Extensions;

/// <summary>
/// Extension methods on <see cref="IMcpServerBuilder"/> that apply shared infrastructure
/// — authentication, structured logging, and exception handling — with a single fluent call.
/// </summary>
public static class McpServerBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="AzureAuthOptions"/> from the <c>AzureAuth</c> configuration
    /// section and makes a preconfigured <see cref="TokenCredential"/> available in DI.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IMcpServerBuilder WithAzureAuth(
        this IMcpServerBuilder builder,
        IConfiguration configuration)
    {
        builder.Services.Configure<AzureAuthOptions>(
            configuration.GetSection(AzureAuthOptions.SectionName));

        builder.Services.AddSingleton<TokenCredential>(sp =>
        {
            var opts = configuration
                .GetSection(AzureAuthOptions.SectionName)
                .Get<AzureAuthOptions>() ?? new AzureAuthOptions();

            return opts.Mode switch
            {
                AuthMode.AzureCli => new AzureCliCredential(),
                AuthMode.ManagedIdentity => new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                    {
                        TenantId = opts.TenantId,
                        ManagedIdentityClientId = opts.ClientId,
                    }),
                _ => new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                    {
                        TenantId = opts.TenantId,
                        ManagedIdentityClientId = opts.ClientId,
                    }),
            };
        });

        return builder;
    }

    /// <summary>
    /// Configures Serilog structured logging that writes to <b>stderr</b>, keeping
    /// stdout exclusively for the MCP wire protocol.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="minimumLevel">The minimum log event level. Defaults to <see cref="LogEventLevel.Information"/>.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IMcpServerBuilder WithStructuredLogging(
        this IMcpServerBuilder builder,
        LogEventLevel minimumLevel = LogEventLevel.Information)
    {
        var logger = McpLoggerFactory.CreateStderrLogger(minimumLevel);
        Log.Logger = logger;

        builder.Services.AddSerilog(logger, dispose: true);

        return builder;
    }

    /// <summary>
    /// Adds a top-level exception handler that serializes unhandled exceptions into a
    /// structured <see cref="McpToolResult{T}"/> JSON payload instead of crashing the server.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IMcpServerBuilder WithExceptionHandling(this IMcpServerBuilder builder)
    {
        builder.Services.AddSingleton<GlobalExceptionHandler>();
        return builder;
    }
}
