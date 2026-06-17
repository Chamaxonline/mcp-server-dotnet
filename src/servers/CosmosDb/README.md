# Cosmos DB MCP Server

An MCP server that exposes Azure Cosmos DB document operations as tools callable by Claude or any MCP-compatible client.

## Available Tools

| Tool | Description |
|------|-------------|
| `query_documents` | Execute a SQL query and return matching documents |
| `get_document` | Retrieve a document by ID and partition key |
| `upsert_document` | Create or replace a document |
| `delete_document` | Delete a document (idempotent) |
| `list_databases` | List all databases and their containers |

## Authentication

### Option A — Account Key

```json
{
  "CosmosDb": {
    "AccountEndpoint": "https://my-account.documents.azure.com:443/",
    "AccountKey": "base64key=="
  }
}
```

### Option B — DefaultAzureCredential (recommended for production)

Assign the **Cosmos DB Built-in Data Contributor** role to the identity on the account:

```bash
az cosmosdb sql role assignment create \
  --account-name my-account \
  --resource-group my-rg \
  --scope "/" \
  --principal-id <identity-object-id> \
  --role-definition-id 00000000-0000-0000-0000-000000000002
```

Then configure:

```json
{
  "CosmosDb": {
    "AccountEndpoint": "https://my-account.documents.azure.com:443/"
  },
  "AzureAuth": { "Mode": "ManagedIdentity" }
}
```

## Configuration

| Key | Required | Description |
|-----|----------|-------------|
| `CosmosDb__AccountEndpoint` | ✅ | Cosmos DB account endpoint URI |
| `CosmosDb__AccountKey` | ⚠️ | Account key (if not using DefaultAzureCredential) |
| `CosmosDb__DefaultDatabase` | | Default database name |
| `CosmosDb__DefaultContainer` | | Default container name |
| `CosmosDb__MaxItemCount` | | Max items per query (default: 100) |

## Running Locally

```bash
export CosmosDb__AccountEndpoint=https://my-account.documents.azure.com:443/
export CosmosDb__AccountKey=mykey==
export CosmosDb__DefaultDatabase=mydb
export CosmosDb__DefaultContainer=items

dotnet run --project src/servers/CosmosDb
```

## Claude Desktop Configuration

```json
{
  "mcpServers": {
    "cosmos-db": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/servers/CosmosDb"],
      "env": {
        "CosmosDb__AccountEndpoint": "https://my-account.documents.azure.com:443/",
        "CosmosDb__AccountKey": "mykey==",
        "CosmosDb__DefaultDatabase": "mydb",
        "CosmosDb__DefaultContainer": "items"
      }
    }
  }
}
```

## Example Prompts

- *"Query all active orders from Cosmos DB"*
- *"Get document with id 'order-123' and partition key 'orders'"*
- *"Create a document: {\"id\": \"item-1\", \"name\": \"Widget\", \"price\": 9.99}"*
- *"What databases and containers are in the Cosmos account?"*
