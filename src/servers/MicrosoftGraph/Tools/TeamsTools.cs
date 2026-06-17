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
/// MCP tools for Microsoft Teams operations via Microsoft Graph.
/// </summary>
[McpServerToolType]
public sealed class TeamsTools
{
    private readonly GraphServiceClient _graph;
    private readonly MicrosoftGraphOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<TeamsTools> _logger;

    /// <summary>Initializes a new instance of <see cref="TeamsTools"/>.</summary>
    public TeamsTools(
        GraphServiceClient graph,
        IOptions<MicrosoftGraphOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<TeamsTools> logger)
    {
        _graph = graph;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Lists the Microsoft Teams teams that the authenticated user is a member of.
    /// </summary>
    [McpServerTool(Name = "list_teams")]
    [Description("Lists the Microsoft Teams that the configured user is a member of. Returns team ID, display name, description, and visibility.")]
    public async Task<string> ListTeamsAsync(
        [Description("The user UPN or object ID. Defaults to the configured UserId ('me').")] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListTeamsAsync),
            async () =>
            {
                var uid = userId ?? _options.UserId;
                _logger.LogInformation("Listing joined teams for user {User}", uid);

                TeamCollectionResponse? teams = uid == "me"
                    ? await _graph.Me.JoinedTeams.GetAsync(cancellationToken: cancellationToken)
                    : await _graph.Users[uid].JoinedTeams.GetAsync(cancellationToken: cancellationToken);

                var items = (teams?.Value ?? []).Select(t => new
                {
                    id = t.Id,
                    displayName = t.DisplayName,
                    description = t.Description,
                    visibility = t.Visibility?.ToString(),
                    webUrl = t.WebUrl,
                }).ToList();

                return McpToolResult.Success<object>(new { count = items.Count, teams = items });
            },
            cancellationToken);
    }

    /// <summary>
    /// Lists the channels within a Microsoft Teams team.
    /// </summary>
    [McpServerTool(Name = "list_channels")]
    [Description("Lists all channels in a Microsoft Teams team. Returns channel ID, display name, description, and membership type.")]
    public async Task<string> ListChannelsAsync(
        [Description("The team ID (obtain from list_teams)")] string teamId,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListChannelsAsync),
            async () =>
            {
                _logger.LogInformation("Listing channels for team {TeamId}", teamId);

                var channels = await _graph.Teams[teamId].Channels
                    .GetAsync(cancellationToken: cancellationToken);

                var items = (channels?.Value ?? []).Select(c => new
                {
                    id = c.Id,
                    displayName = c.DisplayName,
                    description = c.Description,
                    membershipType = c.MembershipType?.ToString(),
                    webUrl = c.WebUrl,
                }).ToList();

                return McpToolResult.Success<object>(new { count = items.Count, channels = items });
            },
            cancellationToken);
    }

    /// <summary>
    /// Retrieves recent messages from a Microsoft Teams channel.
    /// </summary>
    [McpServerTool(Name = "list_channel_messages")]
    [Description("Retrieves recent messages from a Teams channel. Returns message content, author, timestamp, and reaction counts. Requires the ChannelMessage.Read.All permission.")]
    public async Task<string> ListChannelMessagesAsync(
        [Description("The team ID (obtain from list_teams)")] string teamId,
        [Description("The channel ID (obtain from list_channels)")] string channelId,
        [Description("Maximum number of messages to return. Defaults to the configured PageSize.")] int? top = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListChannelMessagesAsync),
            async () =>
            {
                var pageSize = top ?? _options.PageSize;
                _logger.LogInformation(
                    "Listing {Top} messages from channel {ChannelId} in team {TeamId}", pageSize, channelId, teamId);

                var messages = await _graph.Teams[teamId].Channels[channelId].Messages
                    .GetAsync(req =>
                    {
                        req.QueryParameters.Top = pageSize;
                    }, cancellationToken);

                var items = (messages?.Value ?? []).Select(m => new
                {
                    id = m.Id,
                    createdDateTime = m.CreatedDateTime,
                    lastModifiedDateTime = m.LastModifiedDateTime,
                    author = m.From?.User?.DisplayName ?? m.From?.Application?.DisplayName,
                    authorEmail = m.From?.User?.UserIdentityType?.ToString(),
                    body = m.Body?.Content,
                    bodyType = m.Body?.ContentType?.ToString(),
                    importance = m.Importance?.ToString(),
                    webUrl = m.WebUrl,
                    reactionCount = m.Reactions?.Count ?? 0,
                    replyCount = m.Replies?.Count ?? 0,
                }).ToList();

                return McpToolResult.Success<object>(new { count = items.Count, messages = items });
            },
            cancellationToken);
    }

    /// <summary>
    /// Posts a message to a Microsoft Teams channel.
    /// </summary>
    [McpServerTool(Name = "send_channel_message")]
    [Description("Posts a new message to a Microsoft Teams channel. Supports plain text and HTML content. Requires the ChannelMessage.Send permission.")]
    public async Task<string> SendChannelMessageAsync(
        [Description("The team ID (obtain from list_teams)")] string teamId,
        [Description("The channel ID (obtain from list_channels)")] string channelId,
        [Description("The message text content")] string content,
        [Description("Set to true if content is HTML. Defaults to false (plain text).")] bool isHtml = false,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(SendChannelMessageAsync),
            async () =>
            {
                _logger.LogInformation(
                    "Sending message to channel {ChannelId} in team {TeamId}", channelId, teamId);

                var message = new ChatMessage
                {
                    Body = new ItemBody
                    {
                        ContentType = isHtml ? BodyType.Html : BodyType.Text,
                        Content = content,
                    },
                };

                var sent = await _graph.Teams[teamId].Channels[channelId].Messages
                    .PostAsync(message, cancellationToken: cancellationToken);

                return McpToolResult.Success<object>(new
                {
                    sent = true,
                    messageId = sent?.Id,
                    createdDateTime = sent?.CreatedDateTime,
                    webUrl = sent?.WebUrl,
                });
            },
            cancellationToken);
    }
}
