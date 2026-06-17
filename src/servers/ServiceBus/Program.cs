using Azure.Messaging.ServiceBus;
using McpServerDotnet.Core.Extensions;
using McpServerDotnet.Core.Logging;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.ServiceBus.Options;
using McpServerDotnet.Servers.ServiceBus.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

Log.Logger = McpLoggerFactory.CreateStderrLogger(LogEventLevel.Information);

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog(Log.Logger, dispose: true);

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithAzureAuth(builder.Configuration)
        .WithExceptionHandling()
        .WithTools<ServiceBusTools>();

    builder.Services
        .AddOptions<ServiceBusOptions>()
        .Bind(builder.Configuration.GetSection(ServiceBusOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton<GlobalExceptionHandler>();

    // Register the ServiceBusClient, preferring connection string over FQNS.
    builder.Services.AddSingleton<ServiceBusClient>(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;

        if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
        {
            return new ServiceBusClient(opts.ConnectionString);
        }

        if (!string.IsNullOrWhiteSpace(opts.FullyQualifiedNamespace))
        {
            var credential = sp.GetRequiredService<Azure.Core.TokenCredential>();
            return new ServiceBusClient(opts.FullyQualifiedNamespace, credential);
        }

        throw new InvalidOperationException(
            "Either ServiceBus:ConnectionString or ServiceBus:FullyQualifiedNamespace must be configured.");
    });

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service Bus MCP server terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
