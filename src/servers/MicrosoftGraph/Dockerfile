FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY mcp-server-dotnet.sln ./
COPY src/McpServerDotnet.Core/McpServerDotnet.Core.csproj src/McpServerDotnet.Core/
COPY src/servers/MicrosoftGraph/MicrosoftGraph.csproj src/servers/MicrosoftGraph/

RUN dotnet restore src/servers/MicrosoftGraph/MicrosoftGraph.csproj

COPY src/McpServerDotnet.Core/ src/McpServerDotnet.Core/
COPY src/servers/MicrosoftGraph/ src/servers/MicrosoftGraph/

RUN dotnet publish src/servers/MicrosoftGraph/MicrosoftGraph.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

RUN addgroup --system mcp && adduser --system --ingroup mcp mcp
USER mcp

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "McpServer.MicrosoftGraph.dll"]
