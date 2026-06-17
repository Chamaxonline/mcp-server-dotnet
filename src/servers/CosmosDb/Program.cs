using McpServerDotnet.Core.Extensions;
using McpServerDotnet.Core.Logging;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.CosmosDb.Options;
using McpServerDotnet.Servers.CosmosDb.Tools;
using Microsoft.Azure.Cosmos;
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
        .WithTools<CosmosDbTools>();

    builder.Services
        .AddOptions<CosmosDbOptions>()
        .Bind(builder.Configuration.GetSection(CosmosDbOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton<GlobalExceptionHandler>();

    builder.Services.AddSingleton<CosmosClient>(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;

        var clientOptions = new CosmosClientOptions
        {
            AllowBulkExecution = opts.EnableBulkExecution,
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            },
        };

        if (!string.IsNullOrWhiteSpace(opts.AccountKey))
        {
            return new CosmosClient(opts.AccountEndpoint, opts.AccountKey, clientOptions);
        }

        var credential = sp.GetRequiredService<Azure.Core.TokenCredential>();
        return new CosmosClient(opts.AccountEndpoint, credential, clientOptions);
    });

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Cosmos DB MCP server terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
