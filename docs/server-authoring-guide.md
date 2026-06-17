# Server Authoring Guide

This guide walks you through creating a new MCP server from scratch in this monorepo.
By the end you will have a fully working server that registers tools, handles auth, logs
to stderr, and can be deployed as a Docker container.

---

## 1. Create the project

```bash
# From the repo root
mkdir -p src/servers/MyService
dotnet new console \
  --name MyService \
  --framework net9.0 \
  --output src/servers/MyService
```

## 2. Add it to the solution

```bash
dotnet sln mcp-server-dotnet.sln add src/servers/MyService/MyService.csproj
```

## 3. Edit the `.csproj`

Replace the generated `Program.cs` dependencies with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <AssemblyName>McpServer.MyService</AssemblyName>
    <RootNamespace>McpServerDotnet.Servers.MyService</RootNamespace>
    <Description>MCP server for MyService.</Description>
  </PropertyGroup>

  <ItemGroup>
    <!-- Always include these two -->
    <PackageReference Include="ModelContextProtocol" Version="0.1.0-preview.10" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <!-- Add service-specific Azure SDK packages here -->
    <PackageReference Include="Azure.MyService" Version="x.y.z" />
    <!-- Serilog for structured logging to stderr -->
    <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\McpServerDotnet.Core\McpServerDotnet.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

## 4. Create the options class

```
src/servers/MyService/Options/MyServiceOptions.cs
```

```csharp
using System.ComponentModel.DataAnnotations;

namespace McpServerDotnet.Servers.MyService.Options;

public sealed class MyServiceOptions
{
    public const string SectionName = "MyService";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public int MaxResults { get; set; } = 50;
}
```

## 5. Implement tools

Create one file per logical tool group in `Tools/`:

```
src/servers/MyService/Tools/MyServiceTools.cs
```

**Rules:**
- Decorate the class with `[McpServerToolType]`
- Decorate each tool method with `[McpServerTool(Name = "snake_case_name")]`
- Add `[Description("...")]` to the method and each parameter
- Return `Task<string>` — always via `McpToolResult<T>.ToJson()`
- Inject services via constructor; never use `static` state

```csharp
using System.ComponentModel;
using McpServerDotnet.Core;
using McpServerDotnet.Core.Middleware;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace McpServerDotnet.Servers.MyService.Tools;

[McpServerToolType]
public sealed class MyServiceTools
{
    private readonly IMyServiceClient _client;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<MyServiceTools> _logger;

    public MyServiceTools(
        IMyServiceClient client,
        GlobalExceptionHandler exHandler,
        ILogger<MyServiceTools> logger)
    {
        _client = client;
        _exHandler = exHandler;
        _logger = logger;
    }

    [McpServerTool(Name = "do_something")]
    [Description("Does something useful in MyService.")]
    public async Task<string> DoSomethingAsync(
        [Description("The input value")] string input,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(DoSomethingAsync),
            async () =>
            {
                _logger.LogInformation("Doing something with {Input}", input);
                var result = await _client.DoAsync(input, cancellationToken);
                return McpToolResult.Success<object>(result);
            },
            cancellationToken);
    }
}
```

## 6. Wire up `Program.cs`

```csharp
using McpServerDotnet.Core.Extensions;
using McpServerDotnet.Core.Logging;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.MyService.Options;
using McpServerDotnet.Servers.MyService.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        .WithTools<MyServiceTools>();

    builder.Services
        .AddOptions<MyServiceOptions>()
        .Bind(builder.Configuration.GetSection(MyServiceOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton<GlobalExceptionHandler>();
    // Register your service client here
    builder.Services.AddSingleton<IMyServiceClient, MyServiceClient>();

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "MyService MCP server terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

## 7. Add `appsettings.json`

```json
{
  "AzureAuth": {
    "Mode": "ManagedIdentity",
    "TenantId": "",
    "ClientId": ""
  },
  "MyService": {
    "ConnectionString": "",
    "MaxResults": 50
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

## 8. Add the `Dockerfile`

Copy `src/servers/AzureDevOps/Dockerfile` and update the project path references.

## 9. Add a `README.md`

Include:
- Tool table (name | description)
- Authentication section (how to get credentials)
- Configuration table (all env vars with Required/Optional)
- Running locally instructions
- Claude Desktop config JSON snippet
- Example prompts

## 10. Add tests

```bash
mkdir -p tests/servers/MyService.Tests
dotnet new xunit \
  --name MyService.Tests \
  --framework net9.0 \
  --output tests/servers/MyService.Tests
dotnet sln mcp-server-dotnet.sln add tests/servers/MyService.Tests/MyService.Tests.csproj
```

## 11. Update the root README

Add a row to the **Server Directory** table.

---

## Naming Conventions

| Artifact | Convention | Example |
|----------|------------|---------|
| Project name | PascalCase | `MyService` |
| Assembly name | `McpServer.MyService` | `McpServer.CosmosDb.dll` |
| Root namespace | `McpServerDotnet.Servers.MyService` | |
| Tool method names | `VerbNounAsync` | `GetWorkItemAsync` |
| MCP tool names (`Name =`) | `snake_case` | `"get_work_item"` |
| Options section | Service name | `"MyService"` |

## Common Pitfalls

| Pitfall | Fix |
|---------|-----|
| Writing to stdout from application code | Use `ILogger<T>` — Serilog writes to stderr |
| Throwing from a tool method | Wrap with `_exHandler.ExecuteAsync` |
| Hardcoding credentials | Use `appsettings.json` + environment variables |
| Missing `[McpServerToolType]` on the class | The SDK will not discover the tools at startup |
| Returning `null` from a tool | Always return `McpToolResult<T>.ToJson()` |
