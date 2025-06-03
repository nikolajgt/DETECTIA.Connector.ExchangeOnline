using System.Text;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Scan;

public partial class ScanUsersEvents
{
    private record EventAttachmentBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchCount { get; init; }
        internal required EventAttachment Entity { get; init; }
        internal required string EventGraphId { get; init; }
        internal required string UserPrincipalName { get; init; }
    }
    
    public async Task ScanEventAttachmentsAsync(CancellationToken cancellationToken)
    {
        var batchSize = 100;
        var maxDegree = Environment.ProcessorCount;
    
        await DataflowScanPipeline.RunAsync<EventAttachmentBatch, EventAttachment, EventMatch>(
            async (lastId, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.EventAttachments
                    .AsNoTracking()
                    .Where(m => !m.HasBeenScanned && m.Id > lastId)
                    .OrderBy(m => m.Id)
                    .Include(m => m.Event)
                        .ThenInclude(e => e.Organizer)
                    .Select(e => new EventAttachmentBatch
                    {
                        Entity            = e,
                        EventGraphId      = e.Event!.GraphId,
                        UserPrincipalName = e.Event!.Organizer!.UserPrincipalName!
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
                        var attachment = await graph
                            .Users[item.UserPrincipalName]
                            .Events[item.EventGraphId]
                            .Attachments[item.Entity.GraphId]
                            .GetAsync(cancellationToken: ct);
                        
                        if (attachment is null)
                        {
                            logger.LogWarning("Attachment returned null for {item.MessageGraphId}", item.EventGraphId);
                            continue;
                        }
                        
                        var processedMatches = attachment switch
                        {
                            FileAttachment fileAtt => await HandleFileAttachmentAsync(fileAtt, item.Entity, ct),
                            ItemAttachment itemAtt => await HandleItemAttachmentAsync(itemAtt, item.Entity, ct), 
                            ReferenceAttachment refAtt => await HandleReferenceAttachmentAsync(refAtt, item.Entity, ct),
                            _ => []
                        };
                        matches.AddRange(processedMatches);
                    }
                    catch (ODataError ex)
                    {
                        logger.LogWarning(
                            "Skipping scan for {User}/{Msg}: {Error}",
                            item.UserPrincipalName, item.Entity.GraphId, ex.Message
                        );
                    }
                }

                return new DataflowScanPipeline.PipelineScanProcess<EventAttachment, EventMatch>(
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
    
    private async Task<List<EventMatch>> HandleFileAttachmentAsync(
        FileAttachment fileAttachment, 
        EventAttachment entity,
        CancellationToken cancellationToken)
    {
        if(fileAttachment.ContentBytes is null)
        {
            logger.LogWarning("File attachment for {attachmentId} returned null", fileAttachment.Id);
            return [];
        }

        if (!ContentSearch.Infrastructure.Helpers.ContentTypeAndExtensionHelper.GetContentType(
                Path.GetExtension(fileAttachment.Name!).TrimStart('.'), out var attachmentContentType)) return [];
                    
        await using var stream = new MemoryStream(fileAttachment.ContentBytes);
        var resp = await contentSearch.MatchAsync(stream, attachmentContentType, Encoding.UTF8);
        entity.ScannedAt = DateTimeOffset.Now;
        entity.HasBeenScanned = true;
        entity.IsSensitive = resp.Response.IsSensitive; 
        return resp.Response.Matches.GroupBy(obj => new { obj.Name, obj.Pattern })
            .Select(group => new EventMatch
            {
                Name = group.Key.Name,
                Pattern = group.Key.Pattern,
                MatchCount = group.Count(),
                AttachmentId = entity.Id
            }).ToList();
    }

    private Task<List<EventMatch>> HandleItemAttachmentAsync(
        ItemAttachment itemAttachment, 
        EventAttachment entity,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<EventMatch>());
    }

    private Task<List<EventMatch>> HandleReferenceAttachmentAsync(
        ReferenceAttachment referenceAttachment,
        EventAttachment entity,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<EventMatch>());
    }
}