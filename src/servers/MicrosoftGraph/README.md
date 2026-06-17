# Microsoft Graph MCP Server

An MCP server that exposes Microsoft 365 calendar, mail, and Teams operations as tools callable by Claude or any MCP-compatible client.

## Available Tools

### Calendar

| Tool | Description |
|------|-------------|
| `get_calendar_events` | Fetch events within a date range |
| `get_calendar_event` | Get full details of a specific event |
| `list_calendars` | List all calendars for a user |

### Mail

| Tool | Description |
|------|-------------|
| `list_mail_messages` | List messages in inbox or any folder |
| `get_mail_message` | Get full message content and attachments |
| `send_mail_message` | Send an email on behalf of the user |

### Teams

| Tool | Description |
|------|-------------|
| `list_teams` | List teams the user is a member of |
| `list_channels` | List channels in a team |
| `list_channel_messages` | Read recent channel messages |
| `send_channel_message` | Post a message to a channel |

## Authentication

### Option A — App-only (client credentials) — recommended for production

1. Register an app in Azure AD with the following **Application** permissions:
   - `Calendars.Read`
   - `Mail.ReadWrite`
   - `Mail.Send`
   - `ChannelMessage.Read.All`
   - `ChannelMessage.Send`
   - `Team.ReadBasic.All`
   - `Channel.ReadBasic.All`
2. Grant admin consent in the Azure portal.
3. Create a client secret (or use a certificate).

```json
{
  "MicrosoftGraph": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-app-client-id",
    "ClientSecret": "your-client-secret",
    "UserId": "user@yourorg.com"
  }
}
```

### Option B — Delegated (on behalf of signed-in user)

Use `DefaultAzureCredential` (requires interactive login or managed identity with delegated scopes):

```json
{
  "AzureAuth": { "Mode": "AzureCli" },
  "MicrosoftGraph": {
    "TenantId": "your-tenant-id",
    "UserId": "me"
  }
}
```

## Configuration

| Key | Required | Description |
|-----|----------|-------------|
| `MicrosoftGraph__TenantId` | ✅ | Azure AD tenant ID |
| `MicrosoftGraph__ClientId` | ⚠️ | App client ID (app-only auth) |
| `MicrosoftGraph__ClientSecret` | ⚠️ | Client secret (app-only auth) |
| `MicrosoftGraph__UserId` | | Target user UPN or object ID (default: `me`) |
| `MicrosoftGraph__PageSize` | | Items per list call (default: 25) |

## Running Locally

```bash
export MicrosoftGraph__TenantId=your-tenant-id
export MicrosoftGraph__ClientId=your-client-id
export MicrosoftGraph__ClientSecret=your-secret
export MicrosoftGraph__UserId=user@company.com

dotnet run --project src/servers/MicrosoftGraph
```

## Claude Desktop Configuration

```json
{
  "mcpServers": {
    "microsoft-graph": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/servers/MicrosoftGraph"],
      "env": {
        "MicrosoftGraph__TenantId": "your-tenant-id",
        "MicrosoftGraph__ClientId": "your-client-id",
        "MicrosoftGraph__ClientSecret": "your-secret",
        "MicrosoftGraph__UserId": "user@company.com"
      }
    }
  }
}
```

## Example Prompts

- *"What meetings do I have this week?"*
- *"Show me my unread emails from today"*
- *"List all Teams I'm a member of"*
- *"Post 'Deployment complete ✓' to the #releases channel in the Engineering team"*
- *"Send an email to alice@company.com about the project status"*
