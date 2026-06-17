# Azure DevOps MCP Server

An MCP server that exposes Azure DevOps work items, pull requests, and pipeline operations as tools callable by Claude or any MCP-compatible client.

## Available Tools

| Tool | Description |
|------|-------------|
| `get_work_item` | Retrieve a work item by ID (all fields) |
| `create_work_item` | Create a Bug, Task, User Story, Feature, etc. |
| `update_work_item` | Update title, state, description, or assignee |
| `list_pull_requests` | List PRs filtered by status (active/completed/abandoned) |
| `get_pull_request` | Get full PR details including reviewers and commits |
| `create_pull_request` | Open a new PR from a source to a target branch |
| `list_pipelines` | List all pipeline definitions in the project |
| `get_pipeline_run` | Get the result/state of a specific run |
| `trigger_pipeline` | Start a new pipeline run with optional variable overrides |

## Authentication

### Option A — Personal Access Token (recommended for local dev)

1. Create a PAT at `https://dev.azure.com/{org}/_usersSettings/tokens` with **Read & Write** scopes for **Work Items**, **Code**, and **Build**.
2. Set `AzureDevOps__PersonalAccessToken` in your environment or `appsettings.json`.

### Option B — DefaultAzureCredential (recommended for production)

Ensure the identity has the **Project Contributor** role in Azure DevOps. No extra config needed — `DefaultAzureCredential` picks up Managed Identity, environment variables, or CLI sign-in automatically.

## Configuration

| Key | Required | Description |
|-----|----------|-------------|
| `AzureDevOps__Organization` | ✅ | Organization name in `dev.azure.com/{Organization}` |
| `AzureDevOps__Project` | ✅ | Default project name |
| `AzureDevOps__PersonalAccessToken` | ⚠️ | PAT (if not using DefaultAzureCredential) |
| `AzureAuth__Mode` | | `ManagedIdentity` (default) or `ApiKey` or `AzureCli` |

All keys can be set as environment variables using double-underscore as separator (e.g. `AzureDevOps__Organization=myorg`).

## Running Locally

```bash
export AzureDevOps__Organization=myorg
export AzureDevOps__Project=myproject
export AzureDevOps__PersonalAccessToken=mypat

dotnet run --project src/servers/AzureDevOps
```

## Claude Desktop Configuration

```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/src/servers/AzureDevOps"],
      "env": {
        "AzureDevOps__Organization": "myorg",
        "AzureDevOps__Project": "myproject",
        "AzureDevOps__PersonalAccessToken": "mypat"
      }
    }
  }
}
```

### Docker

```bash
docker build -f src/servers/AzureDevOps/Dockerfile -t mcp-azuredevops .

# Claude Desktop config:
# "command": "docker"
# "args": ["run", "-i", "--rm",
#           "-e", "AzureDevOps__Organization=myorg",
#           "-e", "AzureDevOps__Project=myproject",
#           "-e", "AzureDevOps__PersonalAccessToken=mypat",
#           "mcp-azuredevops"]
```

## Example Prompts

- *"Show me work item 4231"*
- *"Create a Bug titled 'Login fails on Safari' and assign it to jane@example.com"*
- *"List the open pull requests in the backend repo"*
- *"Trigger pipeline 42 on the release/v2 branch"*
