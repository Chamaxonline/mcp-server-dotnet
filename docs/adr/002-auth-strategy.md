# ADR 002 — Authentication Strategy

**Status:** Accepted  
**Date:** 2025-06-17  
**Author:** project maintainers

## Context

Each MCP server calls external Azure or M365 services that require authentication. We need a
strategy that:

1. Works in production (Managed Identity, Workload Identity)
2. Works locally for contributors (Azure CLI, IDE sign-in)
3. Supports services that only accept static tokens (Azure DevOps PAT, Service Bus connection strings)
4. Never requires hardcoded secrets in source code

## Decision

Use `Azure.Identity.DefaultAzureCredential` as the default credential, with an opt-out to
`ApiKey` mode for services that don't support Azure AD.

The `AzureAuthOptions` class (in `McpServerDotnet.Core`) captures the selected mode and is
bound from the `AzureAuth` configuration section. A single `TokenCredential` is registered in
DI so all tool classes receive the same credential without instantiating their own.

## Auth Mode Selection

| Mode | Credential Used | When to Use |
|------|-----------------|-------------|
| `ManagedIdentity` (default) | `DefaultAzureCredential` | Production workloads on Azure |
| `ApiKey` | Static string from config | Azure DevOps PAT, raw connection strings |
| `AzureCli` | `AzureCliCredential` | Local dev after `az login` |

## Secrets Management Rules

- Secrets **must not** appear in `appsettings.json` committed to the repo.
- `appsettings.json` ships with blank defaults; values are supplied at runtime via environment variables.
- `.gitignore` excludes `appsettings.local.json` for per-developer overrides.
- CI uses GitHub Actions secrets; local dev uses `.env` files (also gitignored).

## Consequences

- Services that don't support Azure AD (e.g. Azure DevOps without AAD-backed auth) must use
  `ApiKey` mode and supply a PAT or connection string.
- If a consumer uses the server in a non-Azure environment they can set `AzureAuth__Mode=AzureCli`
  to force CLI credential resolution.
- Adding new auth modes (certificate, federated identity) requires extending `AuthMode` enum and
  the credential factory in `McpServerBuilderExtensions.WithAzureAuth`.
