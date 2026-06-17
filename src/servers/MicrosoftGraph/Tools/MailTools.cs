using System.ComponentModel;
using McpServerDotnet.Core;
using McpServerDotnet.Core.Middleware;
using McpServerDotnet.Servers.MicrosoftGraph.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using ModelContextProtocol.Server;

namespace McpServerDotnet.Servers.MicrosoftGraph.Tools;

/// <summary>
/// MCP tools for Microsoft 365 Mail operations via Microsoft Graph.
/// </summary>
[McpServerToolType]
public sealed class MailTools
{
    private readonly GraphServiceClient _graph;
    private readonly MicrosoftGraphOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<MailTools> _logger;

    /// <summary>Initializes a new instance of <see cref="MailTools"/>.</summary>
    public MailTools(
        GraphServiceClient graph,
        IOptions<MicrosoftGraphOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<MailTools> logger)
    {
        _graph = graph;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Lists recent email messages in the user's inbox or a specified mail folder.
    /// </summary>
    [McpServerTool(Name = "list_mail_messages")]
    [Description("Lists recent email messages in a user's mailbox folder. Returns sender, subject, received date, preview, and whether the message is read.")]
    public async Task<string> ListMailMessagesAsync(
        [Description("The mail folder name: 'inbox', 'sentitems', 'drafts', 'deleteditems', etc. Defaults to 'inbox'.")] string folder = "inbox",
        [Description("OData filter expression (e.g. \"isRead eq false\"). Leave empty for no filter.")] string? filter = null,
        [Description("Maximum number of messages to return. Defaults to the configured PageSize.")] int? top = null,
        [Description("The user UPN or object ID. Defaults to the configured UserId ('me').")] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListMailMessagesAsync),
            async () =>
            {
                var uid = userId ?? _options.UserId;
                var pageSize = top ?? _options.PageSize;
                _logger.LogInformation(
                    "Listing {PageSize} messages from {Folder} for user {User}", pageSize, folder, uid);

                MessageCollectionResponse? messages;
                if (uid == "me")
                {
                    messages = await _graph.Me.MailFolders[folder].Messages
                        .GetAsync(req =>
                        {
                            req.QueryParameters.Top = pageSize;
                            req.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime",
                                "isRead", "bodyPreview", "hasAttachments", "importance"];
                            req.QueryParameters.Orderby = ["receivedDateTime desc"];
                            if (!string.IsNullOrWhiteSpace(filter))
                            {
                                req.QueryParameters.Filter = filter;
                            }
                        }, cancellationToken);
                }
                else
                {
                    messages = await _graph.Users[uid].MailFolders[folder].Messages
                        .GetAsync(req =>
                        {
                            req.QueryParameters.Top = pageSize;
                            req.QueryParameters.Select = ["id", "subject", "from", "receivedDateTime",
                                "isRead", "bodyPreview", "hasAttachments", "importance"];
                            req.QueryParameters.Orderby = ["receivedDateTime desc"];
                            if (!string.IsNullOrWhiteSpace(filter))
                            {
                                req.QueryParameters.Filter = filter;
                            }
                        }, cancellationToken);
                }

                var items = (messages?.Value ?? []).Select(m => new
                {
                    id = m.Id,
                    subject = m.Subject,
                    from = m.From?.EmailAddress?.Address,
                    fromName = m.From?.EmailAddress?.Name,
                    receivedDateTime = m.ReceivedDateTime,
                    isRead = m.IsRead,
                    hasAttachments = m.HasAttachments,
                    importance = m.Importance?.ToString(),
                    bodyPreview = m.BodyPreview,
                }).ToList();

                return McpToolResult.Success<object>(new { count = items.Count, messages = items });
            },
            cancellationToken);
    }

    /// <summary>
    /// Retrieves the full content of a specific email message.
    /// </summary>
    [McpServerTool(Name = "get_mail_message")]
    [Description("Retrieves the full content of an email message by ID, including body (HTML or text), all recipients, and attachment names.")]
    public async Task<string> GetMailMessageAsync(
        [Description("The message ID (obtain from list_mail_messages)")] string messageId,
        [Description("The user UPN or object ID. Defaults to the configured UserId ('me').")] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(GetMailMessageAsync),
            async () =>
            {
                var uid = userId ?? _options.UserId;
                _logger.LogInformation("Fetching message {MessageId} for user {User}", messageId, uid);

                Message? message = uid == "me"
                    ? await _graph.Me.Messages[messageId].GetAsync(cancellationToken: cancellationToken)
                    : await _graph.Users[uid].Messages[messageId].GetAsync(cancellationToken: cancellationToken);

                if (message is null)
                {
                    return McpToolResult.Failure<object>($"Message '{messageId}' not found.", "NotFound");
                }

                var result = new
                {
                    id = message.Id,
                    subject = message.Subject,
                    from = message.From?.EmailAddress?.Address,
                    receivedDateTime = message.ReceivedDateTime,
                    sentDateTime = message.SentDateTime,
                    isRead = message.IsRead,
                    importance = message.Importance?.ToString(),
                    body = message.Body?.Content,
                    bodyType = message.Body?.ContentType?.ToString(),
                    toRecipients = (message.ToRecipients ?? [])
                        .Select(r => r.EmailAddress?.Address).ToList(),
                    ccRecipients = (message.CcRecipients ?? [])
                        .Select(r => r.EmailAddress?.Address).ToList(),
                    hasAttachments = message.HasAttachments,
                    attachments = (message.Attachments ?? [])
                        .Select(a => new { name = a.Name, contentType = a.ContentType, size = a.Size })
                        .ToList(),
                };

                return McpToolResult.Success<object>(result);
            },
            cancellationToken);
    }

    /// <summary>
    /// Sends an email message on behalf of the configured user.
    /// </summary>
    [McpServerTool(Name = "send_mail_message")]
    [Description("Sends an email message on behalf of the configured user. The body can be plain text or HTML.")]
    public async Task<string> SendMailMessageAsync(
        [Description("The email subject")] string subject,
        [Description("The message body content")] string body,
        [Description("Comma-separated list of 'To' recipient email addresses")] string toRecipients,
        [Description("Set to true if the body is HTML, false for plain text. Defaults to false.")] bool isHtml = false,
        [Description("Optional comma-separated CC recipient email addresses")] string? ccRecipients = null,
        [Description("The user UPN or object ID. Defaults to the configured UserId ('me').")] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(SendMailMessageAsync),
            async () =>
            {
                var uid = userId ?? _options.UserId;

                static List<Recipient> ParseRecipients(string? csv) =>
                    (csv ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(email => new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = email }
                        })
                        .ToList();

                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = isHtml ? BodyType.Html : BodyType.Text,
                        Content = body,
                    },
                    ToRecipients = ParseRecipients(toRecipients),
                    CcRecipients = ParseRecipients(ccRecipients),
                };

                _logger.LogInformation(
                    "Sending mail '{Subject}' to {Recipients} as user {User}", subject, toRecipients, uid);

                if (uid == "me")
                {
                    var req = new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody { Message = message, SaveToSentItems = true };
                    await _graph.Me.SendMail.PostAsync(req, cancellationToken: cancellationToken);
                }
                else
                {
                    var req = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody { Message = message, SaveToSentItems = true };
                    await _graph.Users[uid].SendMail.PostAsync(req, cancellationToken: cancellationToken);
                }

                return McpToolResult.Success<object>(new { sent = true, subject, to = toRecipients });
            },
            cancellationToken);
    }
}
