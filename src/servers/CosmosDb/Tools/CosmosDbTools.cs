using System.ComponentModel;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using McpServerDotnet.Core;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.CosmosDb.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpServerDotnet.Servers.CosmosDb.Tools;

/// <summary>
/// MCP tools for Azure Cosmos DB document operations.
/// </summary>
[McpServerToolType]
public sealed class CosmosDbTools
{
    private readonly CosmosClient _cosmos;
    private readonly CosmosDbOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<CosmosDbTools> _logger;

    /// <summary>Initializes a new instance of <see cref="CosmosDbTools"/>.</summary>
    public CosmosDbTools(
        CosmosClient cosmos,
        IOptions<CosmosDbOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<CosmosDbTools> logger)
    {
        _cosmos = cosmos;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Executes a SQL query against a Cosmos DB container.
    /// </summary>
    [McpServerTool(Name = "query_documents")]
    [Description("Executes a Cosmos DB SQL query and returns matching documents. Example query: SELECT * FROM c WHERE c.status = 'active' ORDER BY c._ts DESC OFFSET 0 LIMIT 10")]
    public async Task<string> QueryDocumentsAsync(
        [Description("The SQL query to execute, e.g. \"SELECT * FROM c WHERE c.type = 'order'\"")] string query,
        [Description("The container name. Defaults to the configured default container.")] string? container = null,
        [Description("The database name. Defaults to the configured default database.")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(QueryDocumentsAsync),
            async () =>
            {
                var db = database ?? _options.DefaultDatabase;
                var cont = container ?? _options.DefaultContainer;
                var cosmosContainer = _cosmos.GetContainer(db, cont);

                _logger.LogInformation("Executing query on {Database}/{Container}: {Query}", db, cont, query);

                var queryDef = new QueryDefinition(query);
                var options = new QueryRequestOptions { MaxItemCount = _options.MaxItemCount };
                using var iterator = cosmosContainer.GetItemQueryIterator<JsonElement>(queryDef, requestOptions: options);

                var items = new List<JsonElement>();
                double totalRu = 0;

                while (iterator.HasMoreResults)
                {
                    var page = await iterator.ReadNextAsync(cancellationToken);
                    totalRu += page.RequestCharge;
                    items.AddRange(page);

                    if (items.Count >= _options.MaxItemCount)
                    {
                        break;
                    }
                }

                return McpToolResult.Success<object>(new
                {
                    count = items.Count,
                    requestUnitsConsumed = Math.Round(totalRu, 2),
                    items,
                });
            },
            cancellationToken);
    }

    /// <summary>
    /// Retrieves a single Cosmos DB document by its ID and partition key.
    /// </summary>
    [McpServerTool(Name = "get_document")]
    [Description("Retrieves a single Cosmos DB document by its ID and partition key value.")]
    public async Task<string> GetDocumentAsync(
        [Description("The document ID (the 'id' field)")] string id,
        [Description("The partition key value for the document")] string partitionKey,
        [Description("The container name. Defaults to the configured default container.")] string? container = null,
        [Description("The database name. Defaults to the configured default database.")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(GetDocumentAsync),
            async () =>
            {
                var db = database ?? _options.DefaultDatabase;
                var cont = container ?? _options.DefaultContainer;
                var cosmosContainer = _cosmos.GetContainer(db, cont);

                _logger.LogInformation("Reading document {Id} from {Database}/{Container}", id, db, cont);

                try
                {
                    var response = await cosmosContainer.ReadItemAsync<JsonElement>(
                        id,
                        new PartitionKey(partitionKey),
                        cancellationToken: cancellationToken);

                    return McpToolResult.Success<object>(new
                    {
                        document = response.Resource,
                        requestUnitsConsumed = Math.Round(response.RequestCharge, 2),
                        etag = response.ETag,
                    });
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return McpToolResult.Failure<object>(
                        $"Document with id '{id}' and partition key '{partitionKey}' was not found.",
                        "NotFound");
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Creates or replaces a document in a Cosmos DB container.
    /// </summary>
    [McpServerTool(Name = "upsert_document")]
    [Description("Creates or replaces a document in a Cosmos DB container. The document must include an 'id' field. If a document with the same id and partition key exists it is fully replaced.")]
    public async Task<string> UpsertDocumentAsync(
        [Description("The document as a JSON string. Must include an 'id' field.")] string documentJson,
        [Description("The partition key value for the document")] string partitionKey,
        [Description("The container name. Defaults to the configured default container.")] string? container = null,
        [Description("The database name. Defaults to the configured default database.")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(UpsertDocumentAsync),
            async () =>
            {
                var db = database ?? _options.DefaultDatabase;
                var cont = container ?? _options.DefaultContainer;
                var cosmosContainer = _cosmos.GetContainer(db, cont);

                var document = JsonSerializer.Deserialize<JsonElement>(documentJson);
                _logger.LogInformation("Upserting document in {Database}/{Container}", db, cont);

                var response = await cosmosContainer.UpsertItemAsync(
                    document,
                    new PartitionKey(partitionKey),
                    cancellationToken: cancellationToken);

                return McpToolResult.Success<object>(new
                {
                    upserted = true,
                    id = response.Resource.GetProperty("id").GetString(),
                    statusCode = (int)response.StatusCode,
                    requestUnitsConsumed = Math.Round(response.RequestCharge, 2),
                    etag = response.ETag,
                });
            },
            cancellationToken);
    }

    /// <summary>
    /// Deletes a document from a Cosmos DB container by ID and partition key.
    /// </summary>
    [McpServerTool(Name = "delete_document")]
    [Description("Deletes a document from a Cosmos DB container given its ID and partition key. Returns success even if the document did not exist (idempotent).")]
    public async Task<string> DeleteDocumentAsync(
        [Description("The document ID to delete")] string id,
        [Description("The partition key value for the document")] string partitionKey,
        [Description("The container name. Defaults to the configured default container.")] string? container = null,
        [Description("The database name. Defaults to the configured default database.")] string? database = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(DeleteDocumentAsync),
            async () =>
            {
                var db = database ?? _options.DefaultDatabase;
                var cont = container ?? _options.DefaultContainer;
                var cosmosContainer = _cosmos.GetContainer(db, cont);

                _logger.LogInformation("Deleting document {Id} from {Database}/{Container}", id, db, cont);

                try
                {
                    var response = await cosmosContainer.DeleteItemAsync<JsonElement>(
                        id,
                        new PartitionKey(partitionKey),
                        cancellationToken: cancellationToken);

                    return McpToolResult.Success<object>(new
                    {
                        deleted = true,
                        id,
                        requestUnitsConsumed = Math.Round(response.RequestCharge, 2),
                    });
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return McpToolResult.Success<object>(new { deleted = false, id, reason = "NotFound" });
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Lists all databases and their containers in the Cosmos DB account.
    /// </summary>
    [McpServerTool(Name = "list_databases")]
    [Description("Lists all databases and their containers in the connected Cosmos DB account.")]
    public async Task<string> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListDatabasesAsync),
            async () =>
            {
                _logger.LogInformation("Listing Cosmos DB databases");
                using var dbIterator = _cosmos.GetDatabaseQueryIterator<DatabaseProperties>();
                var databases = new List<object>();

                while (dbIterator.HasMoreResults)
                {
                    var page = await dbIterator.ReadNextAsync(cancellationToken);
                    foreach (var db in page)
                    {
                        var containers = new List<string>();
                        var database = _cosmos.GetDatabase(db.Id);
                        using var contIterator = database.GetContainerQueryIterator<ContainerProperties>();

                        while (contIterator.HasMoreResults)
                        {
                            var contPage = await contIterator.ReadNextAsync(cancellationToken);
                            containers.AddRange(contPage.Select(c => c.Id));
                        }

                        databases.Add(new { id = db.Id, containers });
                    }
                }

                return McpToolResult.Success<object>(new { count = databases.Count, databases });
            },
            cancellationToken);
    }
}
