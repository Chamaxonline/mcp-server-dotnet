# Contributing Guide

Welcome! This document is the detailed companion to `CONTRIBUTING.md` at the repo root.
It covers environment setup, code style decisions, and how the CI pipeline works.

## Repository Structure Deep Dive

```
mcp-server-dotnet/
├── src/
│   ├── McpServerDotnet.Core/          # Shared abstractions (no Azure SDK dependency)
│   │   ├── AzureAuthOptions.cs        # Credential strategy enum + options
│   │   ├── McpToolResult.cs           # Discriminated union for tool outcomes
│   │   ├── Extensions/
│   │   │   └── McpServerBuilderExtensions.cs   # Fluent DI helpers
│   │   ├── Logging/
│   │   │   └── McpLoggerFactory.cs    # Serilog stderr factory
│   │   └── Middleware/
│   │       └── GlobalExceptionHandler.cs       # Structured error wrapper
│   └── servers/
│       ├── AzureDevOps/
│       │   ├── Options/               # Strongly-typed config
│       │   ├── Services/              # HTTP client wrapper
│       │   ├── Tools/                 # MCP tool classes (one group per file)
│       │   ├── Program.cs             # Host + DI wiring
│       │   ├── appsettings.json
│       │   ├── Dockerfile
│       │   └── README.md
│       └── ... (other servers follow same layout)
├── tests/
│   ├── McpServerDotnet.Core.Tests/    # Pure unit tests — no external services
│   └── servers/
│       └── AzureDevOps.Tests/         # Can use real APIs gated by environment vars
├── docs/
│   ├── contributing-guide.md          # This file
│   ├── server-authoring-guide.md      # Step-by-step new-server walkthrough
│   └── adr/                           # Architecture Decision Records
└── .github/
    ├── workflows/ci.yml
    └── ISSUE_TEMPLATE/
```

## Running Tests

```bash
# All tests (unit only by default — integration tests require env vars)
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"

# Specific project
dotnet test tests/McpServerDotnet.Core.Tests
```

Integration tests (those requiring real Azure credentials) are gated by the presence of
specific environment variables and will be skipped if not configured.

## Code Style Rules

We enforce these via `.editorconfig`:

1. **File-scoped namespaces** (`namespace Foo.Bar;`, not `namespace Foo.Bar { }`)
2. **XML doc comments** on all public types and members
3. **No `this.` qualifier** unless disambiguation is required
4. **`var` only when type is obvious** from the right-hand side
5. **Trailing newline** at end of every file
6. **No trailing whitespace**

Run `dotnet format` before submitting a PR to auto-fix style issues.

## Branch Strategy

| Branch | Purpose |
|--------|---------|
| `main` | Always deployable; protected by CI |
| `feat/<name>` | New features / new servers |
| `fix/<name>` | Bug fixes |
| `chore/<name>` | Tooling, docs, refactors |
| `release/<version>` | Release preparation |

## CI Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push and PR to `main`:

1. `dotnet restore` — restore NuGet packages
2. `dotnet build --no-restore` — compile everything
3. `dotnet test --no-build` — run unit tests

Integration tests run only when secrets are present (set in repo Actions secrets).

## Releasing

Tags drive NuGet publishing. To release:

1. Update version in `Directory.Build.props`
2. Push a tag: `git tag v1.2.3 && git push --tags`
3. The CI release workflow publishes packages automatically

## Getting Help

- Open a [Discussion](https://github.com/your-org/mcp-server-dotnet/discussions) for questions
- Tag issues with `good first issue` to mark beginner-friendly work
- For security vulnerabilities, use GitHub's private vulnerability reporting — do NOT file a public issue
