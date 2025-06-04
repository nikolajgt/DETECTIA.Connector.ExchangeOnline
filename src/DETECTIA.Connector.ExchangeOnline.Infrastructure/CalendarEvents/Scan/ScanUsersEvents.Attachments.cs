using System.Text;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Scan;

public partial class ScanUsersEvents
{
  
    private sealed record EventAttachmentProcess(EventAttachment Attachment, string EventGraphId, string UserPrincipalName);
    public async Task ScanEventAttachmentsAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        var maxDegree = Environment.ProcessorCount;

        await DataflowScanPipeline.RunAsync<EventAttachmentProcess, EventAttachment, EventMatch>(
            async (lastId, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.EventAttachments
                    .AsNoTracking()
                    .Where(m => !m.HasBeenScanned && m.Id > lastId)
                    .OrderBy(m => m.Id)
                    .Include(m => m.Event)
                    .ThenInclude(e => e!.Organizer)
                    .Select(e => new EventAttachmentProcess(e, e.Event!.GraphId, e.Event!.Organizer!.UserPrincipalName!))
                    .Take(dbFetchPageSize)
                    .ToListAsync(ct);
            },
            async (item, ct) =>
            {
                var matches = new List<EventMatch>();
                try
                {
                    var attachment = await graph
                        .Users[item.UserPrincipalName]
                        .Events[item.EventGraphId]
                        .Attachments[item.Attachment.GraphId]
                        .GetAsync(cancellationToken: ct);

                    var processedMatches = attachment switch
                    {
                        FileAttachment fileAtt => await HandleFileAttachmentAsync(fileAtt, item.Attachment, ct),
                        ItemAttachment itemAtt => await HandleItemAttachmentAsync(itemAtt, item.Attachment, ct),
                        ReferenceAttachment refAtt => await HandleReferenceAttachmentAsync(refAtt, item.Attachment, ct),
                        _ => []
                    };
                    matches.AddRange(processedMatches);
                }
                catch (ODataError ex)
                {
                    logger.LogWarning(
                        "Skipping scan for {User}/{Msg}: {Error}",
                        item.UserPrincipalName, item.Attachment.GraphId, ex.Message
                    );
                }

                return (item.Attachment, matches);
            },
            async (attachments, matches, ct) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (attachments.Any())
                {
                    await db.BulkUpdateAsync(attachments, new BulkConfig
                    {
                        UpdateByProperties = [nameof(UserMessage.Id)],
                        PropertiesToIncludeOnUpdate =
                        [
                            nameof(EventAttachment.HasBeenScanned),
                            nameof(EventAttachment.IsSensitive),
                            nameof(EventAttachment.ScannedAt)
                        ],
                        BatchSize = persistBatchSize
                    }, cancellationToken: ct);
                }

                if (matches.Any())
                {
                    await db.BulkInsertOrUpdateAsync(matches, new BulkConfig
                    {
                        UpdateByProperties =
                            [nameof(EventMatch.EventId), nameof(EventMatch.AttachmentId), nameof(EventMatch.Pattern)],
                        PropertiesToExcludeOnUpdate =
                        [
                            nameof(EventMatch.AttachmentId)
                        ],
                        BatchSize = persistBatchSize
                    }, cancellationToken: ct);
                }
            },
            msg => msg.Attachment.Id,
            persistBatchSize: persistBatchSize,
            maxDegreeOfParallelism: maxDegree,
            cancellationToken: cancellationToken
        );
    }

    private async Task<List<EventMatch>> HandleFileAttachmentAsync(
        FileAttachment fileAttachment,
        EventAttachment entity,
        CancellationToken cancellationToken)
    {
        if (fileAttachment.ContentBytes is null)
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