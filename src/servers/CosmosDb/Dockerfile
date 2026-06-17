FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY mcp-server-dotnet.sln ./
COPY src/McpServerDotnet.Core/McpServerDotnet.Core.csproj src/McpServerDotnet.Core/
COPY src/servers/CosmosDb/CosmosDb.csproj src/servers/CosmosDb/

RUN dotnet restore src/servers/CosmosDb/CosmosDb.csproj

COPY src/McpServerDotnet.Core/ src/McpServerDotnet.Core/
COPY src/servers/CosmosDb/ src/servers/CosmosDb/

RUN dotnet publish src/servers/CosmosDb/CosmosDb.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

RUN groupadd --system mcp && useradd --system --gid mcp --no-create-home mcp
USER mcp

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "McpServer.CosmosDb.dll"]
