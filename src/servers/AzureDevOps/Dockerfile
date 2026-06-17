# ── Build stage ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker layer caching.
COPY mcp-server-dotnet.sln ./
COPY src/McpServerDotnet.Core/McpServerDotnet.Core.csproj src/McpServerDotnet.Core/
COPY src/servers/AzureDevOps/AzureDevOps.csproj src/servers/AzureDevOps/

RUN dotnet restore src/servers/AzureDevOps/AzureDevOps.csproj

# Copy source and publish.
COPY src/McpServerDotnet.Core/ src/McpServerDotnet.Core/
COPY src/servers/AzureDevOps/ src/servers/AzureDevOps/

RUN dotnet publish src/servers/AzureDevOps/AzureDevOps.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# ── Runtime stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# Create a non-root user for security hardening.
RUN addgroup --system mcp && adduser --system --ingroup mcp mcp
USER mcp

COPY --from=build /app/publish .

# MCP servers communicate over STDIO; no port needs to be exposed.
# Run with: docker run -i <image> [args]
ENTRYPOINT ["dotnet", "McpServer.AzureDevOps.dll"]
