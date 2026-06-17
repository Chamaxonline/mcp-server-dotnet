# ADR 001 — MCP SDK Choice

**Status:** Accepted  
**Date:** 2025-06-17  
**Author:** project maintainers

## Context

The Model Context Protocol has multiple community .NET implementations. We need to choose a
single SDK to standardize on so that all servers in this repo share the same tool registration
API, transport handling, and lifecycle model.

## Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **`ModelContextProtocol` (official)** | Official spec compliance, active Microsoft maintenance, consistent with MCP reference implementations | Preview versioning during early development |
| Community fork | Potentially more stable releases | Diverges from spec faster, less support |
| Raw HTTP (no SDK) | Zero dependency | Re-implements protocol; error-prone |

## Decision

Use the official **`ModelContextProtocol`** NuGet package
(package ID: `ModelContextProtocol`, maintained under the `modelcontextprotocol` GitHub org).

## Rationale

- The official SDK is the authoritative implementation of the spec.
- `[McpServerToolType]` / `[McpServerTool]` + `[Description]` attributes provide a clean,
  declarative API that requires minimal boilerplate per tool.
- `WithStdioServerTransport()` and `WithHttpTransport()` abstractions insulate servers from
  transport-layer concerns.
- Microsoft is a primary contributor; the .NET SDK will track the spec closely.

## Consequences

- All servers must reference `ModelContextProtocol` ≥ the version pinned in `Directory.Build.props`.
- When the SDK ships a stable v1, we update the version in one place.
- Tool attribute names (`McpServerTool`, `McpServerToolType`) are SDK-internal; changes require
  updates across all server tool files — acceptable cost for official compliance.
