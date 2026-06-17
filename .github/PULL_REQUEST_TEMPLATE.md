# Pull Request

## Summary

<!--
One sentence: what does this PR do and why?
Example: "Adds the `trigger_pipeline` tool to the AzureDevOps server to allow
Claude to kick off CI builds on demand."
-->

## Type of Change

<!-- Check all that apply -->
- [ ] New MCP server
- [ ] New tool(s) on an existing server
- [ ] Bug fix
- [ ] Refactor / code cleanup
- [ ] Documentation update
- [ ] CI / tooling change
- [ ] Dependency update

## Affected Server(s)

<!-- Which server(s) does this PR touch? -->
- [ ] `McpServerDotnet.Core`
- [ ] `AzureDevOps`
- [ ] `ServiceBus`
- [ ] `CosmosDb`
- [ ] `MicrosoftGraph`
- [ ] Other: ___________

## Checklist

### Code quality
- [ ] All public types and members have XML doc comments
- [ ] Tool methods return `Task<string>` via `McpToolResult<T>.ToJson()`
- [ ] No hardcoded secrets — credentials come from configuration/environment
- [ ] `ILogger<T>` used for logging — no `Console.Write` calls
- [ ] Logging writes to **stderr** (Serilog + `standardErrorFromLevel`)

### New server (if applicable)
- [ ] Project added to `mcp-server-dotnet.sln`
- [ ] `appsettings.json` with blank defaults committed
- [ ] `Dockerfile` follows the multi-stage pattern
- [ ] `README.md` with tool table, auth guide, and Claude Desktop config
- [ ] Server directory table updated in root `README.md`
- [ ] At least one test file in `tests/servers/<ServerName>.Tests/`

### Testing
- [ ] Unit tests pass locally (`dotnet test`)
- [ ] New tools covered by at least a smoke test
- [ ] If integration tests added, they are gated on environment variable presence

### Documentation
- [ ] `README.md` for affected server updated
- [ ] `CONTRIBUTING.md` / authoring guide updated if process changed
- [ ] ADR added if a significant architectural decision was made

## Testing Instructions

<!--
How did you test this? What commands should reviewers run to verify it works?
Include any environment variable setup needed (with dummy/safe values).
-->

```bash
# Example:
export AzureDevOps__Organization=myorg
export AzureDevOps__Project=myproject
export AzureDevOps__PersonalAccessToken=mypat
dotnet run --project src/servers/AzureDevOps
# Then in Claude Desktop, ask: "List my open pull requests in the backend repo"
```

## Related Issues

Closes #
