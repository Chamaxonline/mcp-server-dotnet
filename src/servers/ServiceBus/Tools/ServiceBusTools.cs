using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using McpServerDotnet.Core;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.ServiceBus.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace McpServerDotnet.Servers.ServiceBus.Tools;

/// <summary>
/// MCP tools for Azure Service Bus queue and topic operations.
/// </summary>
[McpServerToolType]
public sealed class ServiceBusTools : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<ServiceBusTools> _logger;

    /// <summary>Initializes a new instance of <see cref="ServiceBusTools"/>.</summary>
    public ServiceBusTools(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<ServiceBusTools> logger)
    {
        _client = client;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to an Azure Service Bus queue or topic.
    /// </summary>
    [McpServerTool(Name = "send_message")]
    [Description("Sends a message to an Azure Service Bus queue or topic. The body can be any string (plain text or JSON).")]
    public async Task<string> SendMessageAsync(
        [Description("The message body to send")] string body,
        [Description("The queue or topic name. Defaults to the configured default entity.")] string? entityName = null,
        [Description("Optional subject / label for the message")] string? subject = null,
        [Description("Optional correlation ID for message tracking")] string? correlationId = null,
        [Description("Optional JSON object of user-defined message properties, e.g. {\"priority\": \"high\"}")] string? propertiesJson = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(SendMessageAsync),
            async () =>
            {
                var entity = entityName ?? _options.DefaultEntityName;
                await using var sender = _client.CreateSender(entity);

                var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(body))
                {
                    Subject = subject,
                    CorrelationId = correlationId,
                    ContentType = "application/json",
                };

                if (propertiesJson is not null)
                {
                    var props = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesJson);
                    if (props is not null)
                    {
                        foreach (var (key, value) in props)
                        {
                            message.ApplicationProperties[key] = value;
                        }
                    }
                }

                _logger.LogInformation("Sending message to entity {Entity}", entity);
                await sender.SendMessageAsync(message, cancellationToken);

                return McpToolResult.Success(new
                {
                    sent = true,
                    entity,
                    messageId = message.MessageId,
                    subject = message.Subject,
                });
            },
            cancellationToken);
    }

    /// <summary>
    /// Peeks at messages in a queue or subscription without consuming them.
    /// Messages remain in the queue after peeking.
    /// </summary>
    [McpServerTool(Name = "peek_messages")]
    [Description("Peeks at up to N messages in a Service Bus queue or subscription without removing them. Returns message ID, body (as UTF-8 string), subject, and properties.")]
    public async Task<string> PeekMessagesAsync(
        [Description("The queue or topic subscription name. Defaults to the configured default entity.")] string? entityName = null,
        [Description("Number of messages to peek. Defaults to the configured default (10).")] int? maxCount = null,
        [Description("The sequence number to start peeking from. Omit to start from the beginning.")] long? fromSequenceNumber = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(PeekMessagesAsync),
            async () =>
            {
                var entity = entityName ?? _options.DefaultEntityName;
                var count = maxCount ?? _options.DefaultPeekCount;
                await using var receiver = _client.CreateReceiver(entity);

                _logger.LogInformation("Peeking {Count} messages from {Entity}", count, entity);

                IReadOnlyList<ServiceBusReceivedMessage> messages = fromSequenceNumber.HasValue
                    ? await receiver.PeekMessagesAsync(count, fromSequenceNumber.Value, cancellationToken)
                    : await receiver.PeekMessagesAsync(count, cancellationToken: cancellationToken);

                var result = messages.Select(m => new
                {
                    messageId = m.MessageId,
                    sequenceNumber = m.SequenceNumber,
                    subject = m.Subject,
                    correlationId = m.CorrelationId,
                    body = Encoding.UTF8.GetString(m.Body),
                    enqueuedAt = m.EnqueuedTime,
                    deliveryCount = m.DeliveryCount,
                    applicationProperties = m.ApplicationProperties,
                }).ToList();

                return McpToolResult.Success(new { count = result.Count, messages = result });
            },
            cancellationToken);
    }

    /// <summary>
    /// Receives and deletes a single message from a queue, completing it immediately.
    /// </summary>
    [McpServerTool(Name = "receive_and_delete_message")]
    [Description("Receives one message from a Service Bus queue using ReceiveAndDelete mode, permanently removing it. Returns the message content or null if the queue is empty.")]
    public async Task<string> ReceiveAndDeleteMessageAsync(
        [Description("The queue name. Defaults to the configured default entity.")] string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ReceiveAndDeleteMessageAsync),
            async () =>
            {
                var entity = entityName ?? _options.DefaultEntityName;
                await using var receiver = _client.CreateReceiver(
                    entity,
                    new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

                _logger.LogInformation("Receiving message from {Entity}", entity);
                var message = await receiver.ReceiveMessageAsync(_options.MaxWaitTime, cancellationToken);

                if (message is null)
                {
                    return McpToolResult.Success<object>(new { received = false, entity });
                }

                return McpToolResult.Success<object>(new
                {
                    received = true,
                    messageId = message.MessageId,
                    sequenceNumber = message.SequenceNumber,
                    subject = message.Subject,
                    body = Encoding.UTF8.GetString(message.Body),
                    enqueuedAt = message.EnqueuedTime,
                    applicationProperties = message.ApplicationProperties,
                });
            },
            cancellationToken);
    }

    /// <summary>
    /// Moves a message to the dead-letter sub-queue with an optional reason.
    /// The message must first be received in PeekLock mode before it can be dead-lettered.
    /// </summary>
    [McpServerTool(Name = "dead_letter_message")]
    [Description("Moves the message identified by the given lock token to the dead-letter sub-queue. Requires the message to have been received in PeekLock mode. Provide the lockToken returned by a PeekLock receive operation.")]
    public async Task<string> DeadLetterMessageAsync(
        [Description("The queue or subscription name. Defaults to the configured default entity.")] string? entityName = null,
        [Description("The lock token of the message to dead-letter (obtained from a PeekLock receive)")] string? lockToken = null,
        [Description("Human-readable reason for dead-lettering")] string? reason = null,
        [Description("Additional error description")] string? errorDescription = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(DeadLetterMessageAsync),
            async () =>
            {
                if (string.IsNullOrWhiteSpace(lockToken))
                {
                    return McpToolResult.Failure<object>(
                        "lockToken is required to dead-letter a message. Receive the message in PeekLock mode first.",
                        "MissingParameter");
                }

                var entity = entityName ?? _options.DefaultEntityName;
                await using var receiver = _client.CreateReceiver(entity);

                _logger.LogInformation(
                    "Dead-lettering message with lock token {LockToken} from {Entity}", lockToken, entity);

                await receiver.DeadLetterMessageAsync(
                    new ServiceBusReceivedMessage(),
                    reason,
                    errorDescription,
                    cancellationToken);

                return McpToolResult.Success<object>(new
                {
                    deadLettered = true,
                    entity,
                    lockToken,
                    reason,
                });
            },
            cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
