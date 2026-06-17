using Azure.Identity;
using McpServerDotnet.Core.Extensions;
using McpServerDotnet.Core.Logging;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.MicrosoftGraph.Options;
using McpServerDotnet.Servers.MicrosoftGraph.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Serilog;
using Serilog.Events;

Log.Logger = McpLoggerFactory.CreateStderrLogger(LogEventLevel.Information);

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(dispose: false);

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithAzureAuth(builder.Configuration)
        .WithExceptionHandling()
        .WithTools<CalendarTools>()
        .WithTools<MailTools>()
        .WithTools<TeamsTools>();

    builder.Services
        .AddOptions<MicrosoftGraphOptions>()
        .Bind(builder.Configuration.GetSection(MicrosoftGraphOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton<GlobalExceptionHandler>();

    builder.Services.AddSingleton<GraphServiceClient>(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<MicrosoftGraphOptions>>().Value;

        // Build the token credential.
        // If a client secret is configured, use client credentials (app-only).
        // Otherwise fall back to DefaultAzureCredential (delegated / managed identity).
        Azure.Core.TokenCredential credential;

        if (!string.IsNullOrWhiteSpace(opts.ClientId) && !string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            credential = new ClientSecretCredential(opts.TenantId, opts.ClientId, opts.ClientSecret);
        }
        else
        {
            credential = sp.GetRequiredService<Azure.Core.TokenCredential>();
        }

        return new GraphServiceClient(credential);
    });

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Microsoft Graph MCP server terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
