using System.ComponentModel;
using System.Text.Json;
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
/// MCP tools for Microsoft 365 Calendar operations via Microsoft Graph.
/// </summary>
[McpServerToolType]
public sealed class CalendarTools
{
    private readonly GraphServiceClient _graph;
    private readonly MicrosoftGraphOptions _options;
    private readonly GlobalExceptionHandler _exHandler;
    private readonly ILogger<CalendarTools> _logger;

    /// <summary>Initializes a new instance of <see cref="CalendarTools"/>.</summary>
    public CalendarTools(
        GraphServiceClient graph,
        IOptions<MicrosoftGraphOptions> options,
        GlobalExceptionHandler exHandler,
        ILogger<CalendarTools> logger)
    {
        _graph = graph;
        _options = options.Value;
        _exHandler = exHandler;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves upcoming calendar events for a user within a specified time window.
    /// </summary>
    [McpServerTool(Name = "get_calendar_events")]
    [Description("Retrieves calendar events for a user within a time range. Returns event subject, start/end times, location, organizer, and online meeting URL if present.")]
    public async Task<string> GetCalendarEventsAsync(
        [Description("ISO 8601 start date-time (e.g. '2025-06-17T00:00:00Z'). Defaults to today midnight UTC.")] string? startDateTime = null,
        [Description("ISO 8601 end date-time (e.g. '2025-06-24T00:00:00Z'). Defaults to 7 days from now.")] string? endDateTime = null,
        [Description("The user UPN or object ID. Defaults to the configured UserId ('me').")] string? userId = null,
        [Description("Maximum number of events to return. Defaults to the configured PageSize.")] int? top = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(GetCalendarEventsAsync),
            async () =>
            {
                var now = DateTimeOffset.UtcNow;
                var start = startDateTime is not null
                    ? DateTimeOffset.Parse(startDateTime)
                    : now.Date;
                var end = endDateTime is not null
                    ? DateTimeOffset.Parse(endDateTime)
                    : now.AddDays(7);

                var uid = userId ?? _options.UserId;
                var pageSize = top ?? _options.PageSize;

                _logger.LogInformation(
                    "Fetching calendar events for {User} from {Start} to {End}", uid, start, end);

                var startStr = start.ToString("yyyy-MM-ddTHH:mm:ssK");
                var endStr = end.ToString("yyyy-MM-ddTHH:mm:ssK");

                EventCollectionResponse? events;
                if (uid == "me")
                {
                    events = await _graph.Me.CalendarView
                        .GetAsync(req =>
                        {
                            req.QueryParameters.StartDateTime = startStr;
                            req.QueryParameters.EndDateTime = endStr;
                            req.QueryParameters.Top = pageSize;
                            req.QueryParameters.Select = ["subject", "start", "end", "location",
                                "organizer", "onlineMeeting", "webLink", "isAllDay", "bodyPreview"];
                            req.QueryParameters.Orderby = ["start/dateTime"];
                        }, cancellationToken);
                }
                else
                {
                    events = await _graph.Users[uid].CalendarView
                        .GetAsync(req =>
                        {
                            req.QueryParameters.StartDateTime = startStr;
                            req.QueryParameters.EndDateTime = endStr;
                            req.QueryParameters.Top = pageSize;
                            req.QueryParameters.Select = ["subject", "start", "end", "location",
                                "organizer", "onlineMeeting", "webLink", "isAllDay", "bodyPreview"];
                            req.QueryParameters.Orderby = ["start/dateTime"];
                        }, cancellationToken);
                }

                var items = (events?.Value ?? []).Select(e => new
                {
                    id = e.Id,
                    subject = e.Subject,
                    start = e.Start?.DateTime,
                    startTimeZone = e.Start?.TimeZone,
                    end = e.End?.DateTime,
                    endTimeZone = e.End?.TimeZone,
                    isAllDay = e.IsAllDay,
                    location = e.Location?.DisplayName,
                    organizer = e.Organizer?.EmailAddress?.Address,
                    bodyPreview = e.BodyPreview,
                    onlineMeetingUrl = e.OnlineMeeting?.JoinUrl,
                    webLink = e.WebLink,
                }).ToList();

                return McpToolResult.Success<object>(new { count = items.Count, events = items });
            },
            cancellationToken);
    }

    /// <summary>
    /// Retrieves the details of a specific calendar event by ID.
    /// </summary>
    [McpServerTool(Name = "get_calendar_event")]
    [Description("Retrieves full details of a specific calendar event by its ID, including attendees and body content.")]
    public async Task<string> GetCalendarEventAsync(
        [Description("The event ID (obtain from get_calendar_events)")] string eventId,
        [Description("The user UPN or object ID. Defaults to the configured UserId ('me').")] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(GetCalendarEventAsync),
            async () =>
            {
                var uid = userId ?? _options.UserId;
                _logger.LogInformation("Fetching event {EventId} for user {User}", eventId, uid);

                Event? ev = uid == "me"
                    ? await _graph.Me.Events[eventId].GetAsync(cancellationToken: cancellationToken)
                    : await _graph.Users[uid].Events[eventId].GetAsync(cancellationToken: cancellationToken);

                if (ev is null)
                {
                    return McpToolResult.Failure<object>($"Event '{eventId}' not found.", "NotFound");
                }

                var result = new
                {
                    id = ev.Id,
                    subject = ev.Subject,
                    body = ev.Body?.Content,
                    bodyType = ev.Body?.ContentType?.ToString(),
                    start = ev.Start?.DateTime,
                    end = ev.End?.DateTime,
                    isAllDay = ev.IsAllDay,
                    location = ev.Location?.DisplayName,
                    organizer = ev.Organizer?.EmailAddress?.Address,
                    attendees = (ev.Attendees ?? []).Select(a => new
                    {
                        email = a.EmailAddress?.Address,
                        name = a.EmailAddress?.Name,
                        type = a.Type?.ToString(),
                        status = a.Status?.Response?.ToString(),
                    }).ToList(),
                    onlineMeetingUrl = ev.OnlineMeeting?.JoinUrl,
                };

                return McpToolResult.Success<object>(result);
            },
            cancellationToken);
    }

    /// <summary>
    /// Lists the calendars available to a user.
    /// </summary>
    [McpServerTool(Name = "list_calendars")]
    [Description("Lists the calendars available to a user, including their names, colors, and whether they can edit them.")]
    public async Task<string> ListCalendarsAsync(
        [Description("The user UPN or object ID. Defaults to the configured UserId ('me').")] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        return await _exHandler.ExecuteAsync<object>(
            nameof(ListCalendarsAsync),
            async () =>
            {
                var uid = userId ?? _options.UserId;
                _logger.LogInformation("Listing calendars for user {User}", uid);

                CalendarCollectionResponse? calendars = uid == "me"
                    ? await _graph.Me.Calendars.GetAsync(cancellationToken: cancellationToken)
                    : await _graph.Users[uid].Calendars.GetAsync(cancellationToken: cancellationToken);

                var items = (calendars?.Value ?? []).Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    color = c.Color?.ToString(),
                    canEdit = c.CanEdit,
                    isDefaultCalendar = c.IsDefaultCalendar,
                    owner = c.Owner?.Address,
                }).ToList();

                return McpToolResult.Success<object>(new { count = items.Count, calendars = items });
            },
            cancellationToken);
    }
}
