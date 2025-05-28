using System.Text;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Scan;

public partial class ScanUsersMailbox
{
    private record AttachmentBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchCount { get; init; }
        internal required MessageAttachment Entity { get; init; }
        internal required string MessageGraphId { get; init; }
        internal required string UserPrincipalName { get; init; }
    }
    
    public async Task ScanUsersMessageAttachmentsAsync(CancellationToken cancellationToken)
    {
        var batchSize = 10;
        
        await DataflowPipeline.RunAsync<AttachmentBatch, MessageAttachment, MessageMatch>(
            async (lastId, ct) => {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.MessageAttachments
                    .AsNoTracking()
                    .Where(x => !x.HasBeenScanned && x.Id > lastId)
                    .OrderBy(x => x.Id)
                    .Include(x => x.Message)
                        .ThenInclude(m => m!.User)
                    .Select(x => new AttachmentBatch {
                        Entity       = x,
                        MessageGraphId   = x.Message!.GraphId,
                        UserPrincipalName= x!.Message!.User!.UserPrincipalName!
                    })
                    .Take(batchSize)
                    .ToListAsync(ct);
            },

            async (batch, ct) => {
                var batchNumber = batch.First().BatchNumber;
                var matches = new List<MessageMatch>();
                foreach (var item in batch)
                {
                    try
                    {
                        var attachment = await graph
                            .Users[item.UserPrincipalName]
                            .Messages[item.MessageGraphId]
                            .Attachments[item.Entity.GraphId]
                            .GetAsync(cancellationToken: ct);
                        
                        if (attachment is null)
                        {
                            logger.LogWarning("Attachment returned null for {item.MessageGraphId}", item.MessageGraphId);
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
                            item.UserPrincipalName, item.Entity.GraphId, ex.Message);
                    }
                }
                logger.LogInformation("Finished scanning batch {}", batchNumber);
                return new DataflowPipeline.PipelineScanProcess<MessageAttachment, MessageMatch>(
                    batch.Select(x => x.Entity).ToList(), 
                    matches);
            },

            async (attachments, ct) => {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (attachments.Entities.Any())
                {
                    await db.BulkUpdateAsync(attachments.Entities, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(MessageAttachment.Id)] ,
                        PropertiesToIncludeOnUpdate =
                        [
                            nameof(MessageAttachment.HasBeenScanned),
                            nameof(MessageAttachment.IsSensitive),
                            nameof(MessageAttachment.ScannedAt)
                        ],
                        BatchSize = batchSize
                    }, cancellationToken: ct);
                }
                if (attachments.Matches.Any())
                {
                    await db.BulkInsertOrUpdateAsync(attachments.Matches, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(MessageMatch.AttachmentId), nameof(MessageMatch.MessageId), nameof(MessageMatch.Pattern)],
                        PropertiesToExcludeOnUpdate = [
                            nameof(MessageMatch.MessageId)
                        ],
                        BatchSize = batchSize
                    }, cancellationToken: ct);
                }
            },

            // 4) keySelector
            x => x.Entity.Id,

            groupBatches:         2,       
            maxDegreeOfParall: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
    }
    
    
    private async Task<List<MessageMatch>> HandleFileAttachmentAsync(
        FileAttachment fileAttachment, 
        MessageAttachment dbItem,
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
        dbItem.ScannedAt = DateTimeOffset.Now;
        dbItem.HasBeenScanned = true;
        dbItem.IsSensitive = resp.Response.IsSensitive; 
        return resp.Response.Matches.GroupBy(obj => new { obj.Name, obj.Pattern })
            .Select(group => new MessageMatch
            {
                Name = group.Key.Name,
                Pattern = group.Key.Pattern,
                MatchCount = group.Count(),
                AttachmentId = dbItem.Id
            }).ToList();
    }

    private Task<List<MessageMatch>> HandleItemAttachmentAsync(
        ItemAttachment itemAttachment, 
        MessageAttachment dbItem,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<MessageMatch>());
    }

    private Task<List<MessageMatch>> HandleReferenceAttachmentAsync(
        ReferenceAttachment referenceAttachment,
        MessageAttachment dbItem,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<MessageMatch>());
    }
}