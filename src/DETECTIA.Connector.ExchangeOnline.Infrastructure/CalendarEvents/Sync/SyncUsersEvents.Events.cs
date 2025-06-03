using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;
using User = DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities.User;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Sync;

public partial class SyncUsersEvents
{
    public async Task SyncUsersEventsAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        var now = DateTime.UtcNow.AddDays(200);
        var weekAgo = now.AddDays(-1000);
        await DataflowSyncPipeline.RunAsync<User, CalendarEvent, User>(
            fetchPageAsync: async (lastUserId, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
                return await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id > lastUserId)
                    .OrderBy(u => u.Id)
                    .Take(dbFetchPageSize)
                    .ToListAsync(ct);
            },
            expandAsync: async (user, ct) =>
            {
                try
                {
                    var builder = graph.Users[user.UserPrincipalName].CalendarView.Delta;
                    if (!string.IsNullOrEmpty(user.EventsDeltaLink))
                        builder = builder.WithUrl(user.EventsDeltaLink);

                    string? nextLink = null;
                    string? deltaLink = null;
                    
                    var calendarEvents = new List<CalendarEvent>();
                    do
                    {
                        var resp = await builder.GetAsDeltaGetResponseAsync(cfg =>
                        {
                            cfg.QueryParameters.StartDateTime = weekAgo.ToString("o");
                            cfg.QueryParameters.EndDateTime = now.ToString("o");
                            cfg.Headers.Add("Prefer", "odata.maxpagesize=100");
                        }, ct);

                        if (resp?.Value is not null)
                        {
                            foreach (var e in resp.Value)
                            {
                                if (e.AdditionalData?.ContainsKey("@removed") == true)
                                    continue;

                                var @event = new CalendarEvent
                                {
                                    GraphId            = e.Id!,
                                    Subject            = e.Subject!,
                                    Start              = ToDateTimeOffset(e.Start),
                                    End                = ToDateTimeOffset(e.End),
                                    LocationDisplayName = e.Location?.DisplayName!,
                                    IsAllDay           = e.IsAllDay,
                                    OrganizerId        = user.Id,
                                    IsCancelled        = e.IsCancelled,
                                    ShowAs             = e.ShowAs?.ToString()!,
                                    BodyContentType    = e.Body?.ContentType?.ToString()!,
                                    Importance         = e.Importance?.ToString()!,
                                    Categories         = e.Categories?.ToList() ?? [],
                                    HasBeenScanned     = false
                                };

                                calendarEvents.Add(@event);
                            }
                        }

                        nextLink  = resp?.OdataNextLink;
                        deltaLink = resp?.OdataDeltaLink;

                        if (nextLink != null)
                            builder = builder.WithUrl(nextLink);

                    } while (nextLink != null);

                    user.EventsDeltaLink = deltaLink;
                    return (calendarEvents, user);
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
                    
                    return ([], user);
                }
            },
            persistBatchAsync: async (events, users, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                // 1. Save calendar events
                await ctx.BulkInsertOrUpdateAsync(events, new BulkConfig
                {
                    UpdateByProperties = [nameof(CalendarEvent.GraphId)],
                    PropertiesToExcludeOnUpdate = [nameof(CalendarEvent.Id)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = persistBatchSize
                }, cancellationToken: ct);
                
                // 5. Save updated delta links for users
                await ctx.BulkInsertOrUpdateAsync(users, new BulkConfig
                {
                    UpdateByProperties = [nameof(User.Id)],
                    PropertiesToInclude = [nameof(User.EventsDeltaLink)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = persistBatchSize
                }, cancellationToken: ct);
            },
            keySelector: u => u.Id,
            persistBatchSize: persistBatchSize,
            maxDegreeOfParallelism: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
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