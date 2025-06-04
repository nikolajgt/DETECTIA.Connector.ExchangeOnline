using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Scan;

public partial class ScanUsersEvents
{
    private record EventProcess(CalendarEvent Event, string UserPrincipalName);
    
    public async Task ScanEventsTextAsync(CancellationToken cancellationToken)
    {
        var batchSize = 100;
        var maxDegree = Environment.ProcessorCount;
    
        await DataflowScanPipeline.RunAsync<EventProcess, CalendarEvent, EventMatch>(
            async (lastId, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.Events
                    .AsNoTracking()
                    .Where(m => !m.HasBeenScanned && m.Id > lastId)
                    .OrderBy(m => m.Id)
                    .Include(m => m.Organizer)
                    .Select(e => new EventProcess(e, e.Organizer!.UserPrincipalName!))
                    .Take(batchSize)
                    .ToListAsync(ct);
            },
    
            async (eventProcess, ct) =>
            {
                var matches = new List<EventMatch>();
                try
                {
                    var graphMsg = await graph
                        .Users[eventProcess.UserPrincipalName]
                        .Events[eventProcess.Event.GraphId]
                        .GetAsync(cfg => 
                                cfg.QueryParameters.Select = ["subject", "body"],
                            ct
                        );
    
                    var body = graphMsg?.Body?.Content ?? string.Empty;
                    var resp = await contentSearch.MatchAsync(body);
                    var convertedMatches = resp.Response.Matches.GroupBy(obj => new { obj.Name, obj.Pattern })
                        .Select(group => new EventMatch
                        {
                            Name = group.Key.Name,
                            Pattern = group.Key.Pattern,
                            MatchCount = group.Count(),
                            EventId  = eventProcess.Event.Id
                        }).ToList();
                    matches.AddRange(convertedMatches);
                    eventProcess.Event.IsSensitive = resp.Response.IsSensitive;
                    eventProcess.Event.ScannedAt = DateTimeOffset.UtcNow;
                }
                catch (ODataError ex)
                {
                    logger.LogWarning(
                        "Skipping scan for {User}/{Event}: {Error}",
                        eventProcess.UserPrincipalName, eventProcess.Event.GraphId, ex.Message
                    );
                }
    
                return (eventProcess.Event, matches);
            },
    
            async (messages, matches, ct) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (messages.Any())
                {
                    await db.BulkUpdateAsync(messages, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(CalendarEvent.Id)] ,
                        PropertiesToIncludeOnUpdate =
                        [
                            nameof(CalendarEvent.HasBeenScanned),
                            nameof(CalendarEvent.IsSensitive),
                            nameof(CalendarEvent.ScannedAt)
                        ],
                        BatchSize = batchSize
                    }, cancellationToken: ct);
                }
                if (matches.Any())
                {
                    await db.BulkInsertOrUpdateAsync(matches, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(EventMatch.EventId), nameof(EventMatch.AttachmentId), nameof(EventMatch.Pattern)],
                        PropertiesToExcludeOnUpdate = [
                            nameof(EventMatch.AttachmentId)
                        ],
                        BatchSize = batchSize
                    }, cancellationToken: ct);
                }
            },
    
            msg => msg.Event.Id,
            persistBatchSize: 500,                       
            maxDegreeOfParallelism: maxDegree,
            cancellationToken: cancellationToken
        );
    }
}