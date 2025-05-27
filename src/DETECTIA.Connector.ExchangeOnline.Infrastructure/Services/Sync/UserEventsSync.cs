using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;
using User = DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities.User;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services.Sync;

public class UserEventsSync(
    ILogger<SyncMetadata> logger, 
    GraphServiceClient graph, 
    IDbContextFactory<AppDatabaseContext> dbFactory)
{
    private const int PersistThreshold = 1000;
    public async Task SyncUsersEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-1000);
            await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
            var users = await dbContext.Users.ToListAsync(cancellationToken);
            var allEvents = new List<CalendarEvent>();
            var allEventParticipants = new List<EventParticipant>();
            foreach (var user in users)
            {
                try
                {
                    // base delta builder for this user's events
                    var builder = graph
                        .Users[user.UserPrincipalName]
                        .CalendarView
                        .Delta;
                    if (!string.IsNullOrEmpty(user.EventsDeltaLink))
                        builder = builder.WithUrl(user.EventsDeltaLink);

                    
                    // if we've stored a previous deltaLink, resume from there
                    if (!string.IsNullOrEmpty(user.EventsDeltaLink))
                        builder = builder.WithUrl(user.EventsDeltaLink);

                    string? nextLink  = null;
                    string? deltaLink = null;

                    do
                    {
                        // fetch one "page" of changes
                        var resp = await builder.GetAsDeltaGetResponseAsync(cfg =>
                        {
                            cfg.QueryParameters.StartDateTime = weekAgo.ToString("o");    
                            cfg.QueryParameters.EndDateTime   = now.ToString("o");        
                        }, cancellationToken);
                        
                        if (resp?.Value != null)
                        {
                            foreach (var e in resp.Value)
                            {
                                // skip deletions
                                if (e.AdditionalData?.ContainsKey("@removed") == true)
                                {
                                    continue;
                                }

                                var startOffset = ToDateTimeOffset(e.Start);
                                var endOffset = ToDateTimeOffset(e.End);

                                var @event = new CalendarEvent
                                {
                                    GraphId = e.Id!,
                                    Subject = e.Subject!,
                                    Start = startOffset,
                                    End = endOffset,
                                    LocationDisplayName = e.Location?.DisplayName!,
                                    IsAllDay = e.IsAllDay,
                                    OrganizerId = user.Id,
                                    IsCancelled = e.IsCancelled,
                                    ShowAs = e.ShowAs?.ToString()!,
                                    BodyContentType = e.Body?.ContentType?.ToString()!,
                                    Importance = e.Importance?.ToString()!,
                                    Categories = e.Categories?.ToList() ?? []
                                };

                                if (e.Attendees is { Count: > 0 })
                                {
                                    // Extract emails from attendees
                                    var attendeeEmails = e.Attendees
                                        .Select(x => x.EmailAddress?.Address)
                                        .Where(x => !string.IsNullOrEmpty(x))
                                        .Distinct()
                                        .ToList();

                                    // Lookup users by Mail
                                    var userIdByMail = await dbContext.Users
                                        .AsNoTracking()
                                        .Where(u => attendeeEmails.Contains(u.Mail!))
                                        .ToDictionaryAsync(u => u.Mail!, u => u.Id, cancellationToken);

                                    // Build EventParticipant list (only for users found)
                                    var participants = e.Attendees
                                        .Where(att => 
                                            !string.IsNullOrEmpty(att?.EmailAddress?.Address) &&
                                            userIdByMail.ContainsKey(att.EmailAddress.Address))
                                        .Select(att => new EventParticipant
                                        {
                                            UserId = userIdByMail[att.EmailAddress!.Address!],
                                            Event = @event,
                                            Type = att.Type?.ToString() ?? "Unknown",
                                            StatusResponse = att.Status?.Response?.ToString() ?? "Unknown"
                                        })
                                        .ToList();

                                    allEventParticipants.AddRange(participants);
                                }
                               
                                allEvents.Add(@event);
                                
                                if (allEvents.Count > PersistThreshold || allEventParticipants.Count > PersistThreshold)
                                {
                                    await FlushAsync(dbContext, allEvents, allEventParticipants, cancellationToken);
                                }
                            }
                        }

                        nextLink  = resp?.OdataNextLink;
                        deltaLink = resp?.OdataDeltaLink;

                        if (nextLink != null)
                            builder = builder.WithUrl(nextLink);

                    } while (nextLink != null);

                    // store the new deltaLink for next time
                //    user.EventsDeltaLink = deltaLink;
                }
                catch (ODataError ex) when (
                    ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("soft-deleted", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("on-premise", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Skipping event sync for {Upn}: {Error}",
                        user.UserPrincipalName,
                        ex.Message);
                }
            }

            await FlushAsync(dbContext, allEvents, allEventParticipants, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }
    
    private async Task FlushAsync(
        AppDatabaseContext dbContext,
        List<CalendarEvent> allEvents, 
        List<EventParticipant> allEventParticipants, 
        CancellationToken cancellationToken)
    {
        if (allEvents.Count > 0)
        {
            await dbContext.BulkInsertOrUpdateAsync(allEvents, new BulkConfig
            {
                UpdateByProperties             = [nameof(CalendarEvent.GraphId) ],
                PropertiesToExcludeOnUpdate    = [nameof(CalendarEvent.GraphId), nameof(CalendarEvent.Id)] ,
                SetOutputIdentity              = false,
                PreserveInsertOrder            = false,
                BatchSize                      = 500
            }, cancellationToken: cancellationToken);
            
            await GetEventIdLookupAsync(dbContext, allEvents,  allEventParticipants,cancellationToken);
            await dbContext.BulkInsertOrUpdateAsync(allEventParticipants, new BulkConfig
            {
                UpdateByProperties             = [nameof(EventParticipant.EventId), nameof(EventParticipant.UserId) ],
                PropertiesToExcludeOnUpdate    = [nameof(EventParticipant.EventId), nameof(EventParticipant.UserId) ],
                SetOutputIdentity              = false,
                PreserveInsertOrder            = false,
                BatchSize                      = PersistThreshold
            }, cancellationToken: cancellationToken);

            allEvents.Clear();
            allEventParticipants.Clear();
        }
    }
    
    private async Task GetEventIdLookupAsync(
        AppDatabaseContext dbContext, 
        List<CalendarEvent> allEvents,
        List<EventParticipant> allEventParticipants, 
        CancellationToken cancellationToken)
    {
        var lookup = allEvents.Select(x => x!.GraphId);
        var response = await dbContext.Events
            .AsNoTracking()
            .Where(e => lookup.Contains(e.GraphId))
            .ToDictionaryAsync(e => e.GraphId, e => e.Id, cancellationToken: cancellationToken);
        
        foreach (var participant in allEventParticipants)
        {
            var graphId = participant.Event?.GraphId;

            if (graphId != null && response.TryGetValue(graphId, out var dbEventId))
            {
                participant.EventId = dbEventId;
            }
            else
            {
                throw new InvalidOperationException($"Event ID not found for GraphId: {graphId}");
            }

            participant.Event = null;
        }
    }

    private static DateTimeOffset? ToDateTimeOffset(DateTimeTimeZone? dtz)
    {
        if(!string.IsNullOrWhiteSpace(dtz?.DateTime)) return null;
        var local = DateTime.Parse(dtz.DateTime); 
        var tz   = TimeZoneInfo.FindSystemTimeZoneById(dtz?.TimeZone!);
        var offset = tz.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }
}