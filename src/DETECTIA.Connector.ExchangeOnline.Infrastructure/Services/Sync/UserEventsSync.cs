using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Event = DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities.Event;
using Task = System.Threading.Tasks.Task;
using User = DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities.User;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services.Sync;

public class UserEventsSync(
    ILogger<SyncMetadata> logger, 
    GraphServiceClient graph, 
    IDbContextFactory<AppDatabaseContext> dbFactory)
{
    public async Task SyncUsersEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
            var users = await dbContext.Users.ToListAsync(cancellationToken);
            
            var userByUpn = users
                .ToDictionary(u => u.UserPrincipalName!, StringComparer.OrdinalIgnoreCase);
            var allEvents = new List<Event>();
            var allEventParticipants = new List<EventParticipant>();
            foreach (var user in users)
            {
                try
                {
                    // base delta builder for this user's events
                    var baseBuilder = graph
                        .Users[user.UserPrincipalName]
                        .Events
                        .Delta;

                    // if we've stored a previous deltaLink, resume from there
                    var builder = string.IsNullOrEmpty(user.EventsDeltaLink)
                        ? baseBuilder
                        : baseBuilder.WithUrl(user.EventsDeltaLink);

                    string? nextLink  = null;
                    string? deltaLink = null;

                    do
                    {
                        // fetch one "page" of changes
                        var resp = await builder.GetAsDeltaGetResponseAsync(cancellationToken: cancellationToken);

                        if (resp?.Value != null)
                        {
                            foreach (var e in resp.Value)
                            {
                                // skip deletions
                                if (e.AdditionalData?.ContainsKey("@removed") == true)
                                    continue;

                                var startOffset = ToDateTimeOffset(e.Start);
                                var endOffset = ToDateTimeOffset(e.End);

                                var @event = new Event
                                {
                                    GraphId = e.Id!,
                                    Subject = e.Subject!,
                                    Start = startOffset,
                                    End = endOffset,
                                    LocationDisplayName = e.Location?.DisplayName!,
                                    IsAllDay = e.IsAllDay,
                                    IsCancelled = e.IsCancelled,
                                    ShowAs = e.ShowAs?.ToString()!,
                                    BodyContentType = e.Body?.ContentType?.ToString()!,
                                    Organizer = user,
                                    Importance = e.Importance?.ToString()!,
                                    Categories = e.Categories?.ToList() ?? []
                                };
                                var participants = e.Attendees?
                                    .Where(att => !string.IsNullOrEmpty(att.EmailAddress.Address))
                                    .Where(att => userByUpn.ContainsKey(att.EmailAddress.Address))
                                    .Select(att => new EventParticipant
                                    {
                                        Event = @event,
                                        User       = user,
                                        Type       = att.Type!.ToString(),
                                        StatusResponse = att.Status!.ToString(),
                                    }).ToList() ?? [];
                                    

                                allEvents.Add(@event);
                                allEventParticipants.AddRange(participants);
                            }
                        }

                        nextLink  = resp?.OdataNextLink;
                        deltaLink = resp?.OdataDeltaLink;

                        if (nextLink != null)
                            builder = builder.WithUrl(nextLink);

                    } while (nextLink != null);

                    // store the new deltaLink for next time
                    user.EventsDeltaLink = deltaLink;
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

            await dbContext.BulkInsertOrUpdateAsync(allEvents, new BulkConfig
            {
                UpdateByProperties             = [nameof(Event.GraphId) ],
                PropertiesToExcludeOnUpdate    = [nameof(Event.GraphId), nameof(Event.Id)] ,
                SetOutputIdentity              = false,
                PreserveInsertOrder            = false,
                BatchSize                      = 500
            }, cancellationToken: cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
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