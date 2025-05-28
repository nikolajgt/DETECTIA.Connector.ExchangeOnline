using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using DETECTIA.Connector.ExchangeOnline.Migration;
using DETECTIA.ContentSearch.Application;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Scan;

public partial class ScanUsersEvents
{
    private record EventBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchCount { get; init; }
        internal required CalendarEvent Entity { get; init; }
        internal required string UserPrincipalName { get; init; }
    }
    
    public async Task ScanEventsTextAsync(CancellationToken cancellationToken)
    {
        var batchSize = 100;
        var maxDegree = Environment.ProcessorCount;
    
        await DataflowPipeline.RunAsync<EventBatch, CalendarEvent, EventMatch>(
            async (lastId, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.Events
                    .AsNoTracking()
                    .Where(m => !m.HasBeenScanned && m.Id > lastId)
                    .OrderBy(m => m.Id)
                    .Include(m => m.Organizer)
                    .Select(e => new EventBatch
                    {
                        Entity            = e,
                        UserPrincipalName= e.Organizer!.UserPrincipalName!
                    })
                    .Take(batchSize)
                    .ToListAsync(ct);
            },
    
            async (batch, ct) =>
            {
                var matches = new List<EventMatch>();
                foreach (var item in batch)
                {
                    try
                    {
                        var graphMsg = await graph
                            .Users[item.UserPrincipalName]
                            .Messages[item.Entity.GraphId]
                            .GetAsync(cfg => 
                                cfg.QueryParameters.Select = ["subject", "body"],
                                ct
                            );
    
                        var body = graphMsg?.Body?.Content ?? string.Empty;
                        if(string.IsNullOrWhiteSpace(body)) continue;
                        var resp = await contentSearch.MatchAsync(body);
                        var convertedMatches = resp.Response.Matches.GroupBy(obj => new { obj.Name, obj.Pattern })
                            .Select(group => new EventMatch
                            {
                                Name = group.Key.Name,
                                Pattern = group.Key.Pattern,
                                MatchCount = group.Count(),
                                EventId  = item.Entity.Id
                            }).ToList();
                        matches.AddRange(convertedMatches);
                        item.Entity.IsSensitive = resp.Response.IsSensitive;
                        item.Entity.ScannedAt = DateTimeOffset.UtcNow;
                    }
                    catch (ODataError ex)
                    {
                        logger.LogWarning(
                            "Skipping scan for {User}/{Msg}: {Error}",
                            item.UserPrincipalName, item.Entity.GraphId, ex.Message
                        );
                    }
                }
    
                return new DataflowPipeline.PipelineScanProcess<CalendarEvent, EventMatch>(
                    batch.Select(x => x.Entity).ToList(),
                    matches);
            },
    
            async (messages, ct) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (messages.Entities.Any())
                {
                    await db.BulkUpdateAsync(messages.Entities, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(UserMessage.Id)] ,
                        PropertiesToIncludeOnUpdate =
                        [
                            nameof(UserMessage.HasBeenScanned),
                            nameof(UserMessage.IsSensitive),
                            nameof(UserMessage.ScannedAt)
                        ],
                        BatchSize = batchSize
                    }, cancellationToken: ct);
                }
                if (messages.Matches.Any())
                {
                    await db.BulkInsertOrUpdateAsync(messages.Matches, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(EventMatch.EventId), nameof(EventMatch.AttachmentId), nameof(EventMatch.Pattern)],
                        PropertiesToExcludeOnUpdate = [
                            nameof(EventMatch.AttachmentId)
                        ],
                        BatchSize = batchSize
                    }, cancellationToken: ct);
                }
            },
    
            msg => msg.Entity.Id,
    
            groupBatches:         1,                       
            maxDegreeOfParall: maxDegree,
            cancellationToken: cancellationToken
        );
    }
}