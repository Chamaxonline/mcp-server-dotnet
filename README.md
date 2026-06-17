# mcp-server-dotnet

![Build](https://img.shields.io/github/actions/workflow/status/your-org/mcp-server-dotnet/ci.yml?branch=main&label=build)
![License](https://img.shields.io/github/license/your-org/mcp-server-dotnet)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![MCP](https://img.shields.io/badge/MCP-1.0-blue)

> A community-maintained collection of **Model Context Protocol (MCP) server implementations** written in C# / .NET 9, targeting Azure and developer-ecosystem services.

---

## What is this?

[Model Context Protocol](https://modelcontextprotocol.io) is an open standard that lets AI assistants (like Claude) securely call tools that integrate with external services. This repo provides production-grade MCP servers for popular Azure and developer services, each packaged as a standalone .NET 9 executable that can be run locally or in a container.

## Server Directory

| Server | Services | Status | Docker Image |
|--------|----------|--------|--------------|
| [AzureDevOps](src/servers/AzureDevOps/README.md) | Work items, PRs, Pipelines | ✅ Stable | `ghcr.io/your-org/mcp-server-azuredevops:latest` |
| [ServiceBus](src/servers/ServiceBus/README.md) | Send, peek, dead-letter | ✅ Stable | `ghcr.io/your-org/mcp-server-servicebus:latest` |
| [CosmosDb](src/servers/CosmosDb/README.md) | Query, upsert, delete | ✅ Stable | `ghcr.io/your-org/mcp-server-cosmosdb:latest` |
| [MicrosoftGraph](src/servers/MicrosoftGraph/README.md) | Calendar, mail, Teams | ✅ Stable | `ghcr.io/your-org/mcp-server-msgraph:latest` |

## Quickstart

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- [Claude Desktop](https://claude.ai/download) (or any MCP-compatible client)
- Azure credentials (see [Authentication](#authentication))

### Run a server locally

```bash
# Clone the repo
git clone https://github.com/your-org/mcp-server-dotnet
cd mcp-server-dotnet

# Set up environment variables (see each server's README)
export AzureDevOps__Organization=myorg
export AzureDevOps__Project=myproject
export AzureDevOps__PersonalAccessToken=<your-pat>

# Run the server
dotnet run --project src/servers/AzureDevOps
```

### Add to Claude Desktop

Edit `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or `%APPDATA%\Claude\claude_desktop_config.json` (Windows):

```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/mcp-server-dotnet/src/servers/AzureDevOps"],
      "env": {
        "AzureDevOps__Organization": "myorg",
        "AzureDevOps__Project": "myproject",
        "AzureDevOps__PersonalAccessToken": "your-pat"
      }
    }
  }
}
```

### Run with Docker

```bash
docker run -i \
  -e AzureDevOps__Organization=myorg \
  -e AzureDevOps__Project=myproject \
  -e AzureDevOps__PersonalAccessToken=your-pat \
  ghcr.io/your-org/mcp-server-azuredevops:latest
```

Claude Desktop config using Docker:

```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "AzureDevOps__Organization=myorg",
        "-e", "AzureDevOps__Project=myproject",
        "-e", "AzureDevOps__PersonalAccessToken=your-pat",
        "ghcr.io/your-org/mcp-server-azuredevops:latest"
      ]
    }
  }
}
```

## Authentication

All servers support two authentication modes:

**Managed Identity / DefaultAzureCredential** (recommended for production):
```json
{
  "AzureAuth": {
    "Mode": "ManagedIdentity"
  }
}
```
Works automatically with Azure Managed Identity, Azure CLI (`az login`), environment variables (`AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`), and Visual Studio sign-in.

**API Key / PAT** (for local development):
```json
{
  "AzureAuth": {
    "Mode": "ApiKey",
    "ApiKey": "your-token-here"
  }
}
```

See each server's README for service-specific auth configuration.

## Repository Structure

```
mcp-server-dotnet/
├── src/
│   ├── McpServerDotnet.Core/          # Shared abstractions, auth, logging
│   └── servers/
│       ├── AzureDevOps/               # Azure DevOps MCP server
│       ├── ServiceBus/                # Azure Service Bus MCP server
│       ├── CosmosDb/                  # Cosmos DB MCP server
│       └── MicrosoftGraph/            # Microsoft Graph MCP server
├── tests/
│   ├── McpServerDotnet.Core.Tests/    # Unit tests for core library
│   └── servers/
│       └── AzureDevOps.Tests/         # Integration test skeleton
├── docs/
│   ├── contributing-guide.md
│   ├── server-authoring-guide.md
│   └── adr/                           # Architecture Decision Records
├── .github/
│   ├── workflows/ci.yml
│   └── ISSUE_TEMPLATE/
└── mcp-server-dotnet.sln
```

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) to get started.
Adding a new server is documented step-by-step in [docs/server-authoring-guide.md](docs/server-authoring-guide.md).

## License

[MIT](LICENSE) — free to use, modify, and distribute.
