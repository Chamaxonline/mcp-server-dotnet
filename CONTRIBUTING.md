# Contributing to mcp-server-dotnet

Thank you for your interest in contributing! This document describes the workflow for contributing to the project, from filing issues to submitting pull requests.

## Code of Conduct

This project follows the [Contributor Covenant](https://www.contributor-covenant.org). Please be respectful in all interactions.

## Ways to Contribute

- **Bug reports** — use the [Bug Report](.github/ISSUE_TEMPLATE/bug_report.yml) template
- **New server requests** — use the [New Server Request](.github/ISSUE_TEMPLATE/new_server_request.yml) template
- **Code contributions** — follow the steps below

---

## Development Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Docker | 24+ (optional, for container testing) |
| Git | 2.40+ |

---

## Setting Up the Development Environment

```bash
# 1. Fork and clone
git clone https://github.com/<your-fork>/mcp-server-dotnet
cd mcp-server-dotnet

# 2. Restore dependencies
dotnet restore

# 3. Build everything
dotnet build

# 4. Run all tests
dotnet test
```

---

## Adding a New Server — Step-by-Step

The detailed guide is in [docs/server-authoring-guide.md](docs/server-authoring-guide.md). The short version:

### 1. Create the project

```bash
mkdir -p src/servers/MyService
cd src/servers/MyService
dotnet new console -n MyService --framework net9.0
```

### 2. Add project to the solution

```bash
cd ../../..   # repo root
dotnet sln add src/servers/MyService/MyService.csproj
```

### 3. Add required NuGet references

```xml
<ItemGroup>
  <PackageReference Include="ModelContextProtocol" Version="0.1.0-preview.10" />
  <ProjectReference Include="..\..\McpServerDotnet.Core\McpServerDotnet.Core.csproj" />
  <!-- Add Azure SDK or other service packages here -->
</ItemGroup>
```

### 4. Implement your tools

- Create an `Options/` folder with a strongly-typed options class
- Create a `Tools/` folder with one file per logical tool group
- Annotate tool methods with `[McpServerTool]` and `[Description]`
- Each method must return `Task<string>` using `McpToolResult<T>.ToJson()`

### 5. Wire up `Program.cs`

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithAzureAuth(builder.Configuration)
    .WithTools<MyServiceTools>();

builder.Services.Configure<MyServiceOptions>(
    builder.Configuration.GetSection(MyServiceOptions.SectionName));
builder.Services.AddSingleton<IMyServiceClient, MyServiceClient>();

await builder.Build().RunAsync();
```

### 6. Add required files

- `appsettings.json` — configuration schema with sensible defaults
- `README.md` — setup instructions, tool list, Claude Desktop config example
- `Dockerfile` — follows the pattern in existing servers

### 7. Add a test skeleton

```bash
mkdir -p tests/servers/MyService.Tests
cd tests/servers/MyService.Tests
dotnet new xunit -n MyService.Tests --framework net9.0
dotnet sln ../../../mcp-server-dotnet.sln add MyService.Tests.csproj
```

### 8. Update the root README

Add a row to the Server Directory table in `README.md`.

### 9. Open a Pull Request

Use the PR template (`.github/PULL_REQUEST_TEMPLATE.md`). CI runs automatically.

---

## Coding Conventions

- File-scoped namespaces (`namespace Foo.Bar;`)
- XML doc comments on all public types and members
- Tool methods always return `Task<string>` using `McpToolResult<T>.ToJson()`
- Use `ILogger<T>` injected via constructor — never `Console.Write`
- Log to stderr (Serilog `standardErrorFromLevel`) so stdout stays clean for MCP
- No hardcoded secrets — use `appsettings.json` + environment variables

## Commit Message Convention

```
<type>(<scope>): <short summary>

Types: feat | fix | docs | refactor | test | chore
Scope: core | azdevops | servicebus | cosmosdb | msgraph | ci

Example:
feat(azdevops): add CreateWorkItem tool
fix(cosmosdb): handle 404 gracefully in GetDocument
```

## Questions?

Open a [Discussion](https://github.com/your-org/mcp-server-dotnet/discussions) or ask in the relevant GitHub Issue.
