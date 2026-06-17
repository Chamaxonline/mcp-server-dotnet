using McpServerDotnet.Core.Extensions;
using McpServerDotnet.Core.Logging;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.AzureDevOps.Options;
using McpServerDotnet.Servers.AzureDevOps.Services;
using McpServerDotnet.Servers.AzureDevOps.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

// Bootstrap a minimal logger before Host is built so startup errors are visible on stderr.
Log.Logger = McpLoggerFactory.CreateStderrLogger(LogEventLevel.Information);

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Replace default Microsoft logging with Serilog writing to stderr.
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(dispose: false);

    // Register MCP server with STDIO transport and all Azure DevOps tools.
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithAzureAuth(builder.Configuration)
        .WithExceptionHandling()
        .WithTools<WorkItemTools>()
        .WithTools<PullRequestTools>()
        .WithTools<PipelineTools>();

    // Bind strongly-typed options.
    builder.Services
        .AddOptions<AzureDevOpsOptions>()
        .Bind(builder.Configuration.GetSection(AzureDevOpsOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Register the shared HTTP client used by all tools.
    builder.Services.AddHttpClient<AzureDevOpsHttpClient>();

    // Register the cross-cutting exception handler.
    builder.Services.AddSingleton<GlobalExceptionHandler>();

    var host = builder.Build();
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Azure DevOps MCP server terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
