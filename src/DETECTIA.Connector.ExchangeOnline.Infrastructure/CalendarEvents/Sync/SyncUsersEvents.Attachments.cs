using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Sync;

public partial class SyncUsersEvents
{
    public async Task SyncCalendarEventAttachmentsAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        await DataflowSyncPipeline.RunAsync<CalendarEvent, EventAttachment>(
            fetchPageAsync: async (lastKey, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
                return await dbContext.Events
                    .AsNoTracking()
                    .Where(e => e.Id > lastKey)
                    .Where(e => !e.HasBeenScanned)
                    .Where(e => !string.IsNullOrEmpty(e.GraphId))
                    .Include(e => e.Organizer)
                    .OrderBy(e => e.Id)
                    .Take(dbFetchPageSize)
                    .ToListAsync(ct);
            },
            expandAsync: async (ev, ct) =>
            {
                try
                {
                    var attachments = await graph.Users[ev.Organizer!.UserPrincipalName]
                        .Events[ev.GraphId]
                        .Attachments
                        .GetAsync(cancellationToken: ct);

                    if (attachments?.Value == null)
                        return [];

                    return attachments.Value
                        .OfType<Microsoft.Graph.Models.FileAttachment>()
                        .Select(att => new EventAttachment
                        {
                            GraphId        = att.Id!,
                            Size           = att.Size,
                            EventId        = ev.Id,
                            Name           = att.Name!,
                            ContentType    = att.ContentType!,
                            IsInline       = att.IsInline ?? false,
                            HasBeenScanned = false
                        })
                        .ToList();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to fetch attachments for Event {GraphId}", ev.GraphId);
                    return [];
                }
            },
            persistBatchAsync: async (batch, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                await ctx.BulkInsertOrUpdateAsync(batch, new BulkConfig
                {
                    UpdateByProperties = [nameof(EventAttachment.GraphId)],
                    PropertiesToExcludeOnUpdate = [nameof(EventAttachment.Id)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = persistBatchSize
                }, cancellationToken: ct);
            },
            keySelector: e => e.Id,
            persistBatchSize: persistBatchSize,
            maxDegreeOfParallelism: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
    }

}