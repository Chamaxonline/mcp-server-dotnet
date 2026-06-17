# Azure Service Bus MCP Server

An MCP server that exposes Azure Service Bus queue and topic operations as tools callable by Claude or any MCP-compatible client.

## Available Tools

| Tool | Description |
|------|-------------|
| `send_message` | Send a message to a queue or topic with optional properties |
| `peek_messages` | Non-destructively peek at N messages |
| `receive_and_delete_message` | Receive and permanently remove a message |
| `dead_letter_message` | Move a PeekLock-received message to the dead-letter queue |

## Authentication

### Option A — Connection String

```json
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://my-namespace.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=..."
  }
}
```

### Option B — DefaultAzureCredential (recommended for production)

Assign the identity the **Azure Service Bus Data Owner** (or more restrictive) role on the namespace, then configure:

```json
{
  "ServiceBus": {
    "FullyQualifiedNamespace": "my-namespace.servicebus.windows.net"
  },
  "AzureAuth": {
    "Mode": "ManagedIdentity"
  }
}
```

## Configuration

| Key | Required | Description |
|-----|----------|-------------|
| `ServiceBus__FullyQualifiedNamespace` | ⚠️ | FQNS when using DefaultAzureCredential |
| `ServiceBus__ConnectionString` | ⚠️ | Full connection string (alternative to FQNS) |
| `ServiceBus__DefaultEntityName` | | Default queue/topic name |
| `ServiceBus__DefaultPeekCount` | | Max messages to peek (default: 10) |

## Running Locally

```bash
export ServiceBus__FullyQualifiedNamespace=my-namespace.servicebus.windows.net
export ServiceBus__DefaultEntityName=my-queue

dotnet run --project src/servers/ServiceBus
```

## Claude Desktop Configuration

```json
{
  "mcpServers": {
    "service-bus": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/servers/ServiceBus"],
      "env": {
        "ServiceBus__FullyQualifiedNamespace": "my-namespace.servicebus.windows.net",
        "ServiceBus__DefaultEntityName": "my-queue"
      }
    }
  }
}
```

## Example Prompts

- *"Send a message 'Order received' to the orders queue"*
- *"Peek at the next 5 messages in the dead-letter queue"*
- *"Are there any messages waiting in the notifications queue?"*
