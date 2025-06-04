using System.Text;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Scan;

public partial class ScanUsersMailbox
{
    private sealed record MessageAttachmentProcess(MessageAttachment Attachment, string MessageGraphId, string UserPrincipalName);
    
    public async Task ScanUsersMessageAttachmentsAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        await DataflowScanPipeline.RunAsync<MessageAttachmentProcess, MessageAttachment, MessageMatch>(
            async (lastId, ct) => {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.MessageAttachments
                    .AsNoTracking()
                    .Where(x => !x.HasBeenScanned && x.Id > lastId)
                    .OrderBy(x => x.Id)
                    .Include(x => x.Message)
                        .ThenInclude(m => m!.User)
                    .Select(x => new MessageAttachmentProcess(x, x.Message!.GraphId, x!.Message!.User!.UserPrincipalName!))
                    .Take(dbFetchPageSize)
                    .ToListAsync(ct);
            },

            async (item, ct) => {
                var matches = new List<MessageMatch>();
                try
                {
                    var attachment = await graph
                        .Users[item.UserPrincipalName]
                        .Messages[item.MessageGraphId]
                        .Attachments[item.Attachment.GraphId]
                        .GetAsync(cancellationToken: ct);
                        
                    if (attachment is null)
                    {
                        logger.LogWarning("Attachment returned null for {item.MessageGraphId}", item.MessageGraphId);
                    }
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
                        item.UserPrincipalName, item.Attachment.GraphId, ex.Message);
                }

                return (item.Attachment, matches);
            },

            async (attachments, matches, ct) => {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (attachments.Any())
                {
                    await db.BulkUpdateAsync(attachments, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(MessageAttachment.Id)] ,
                        PropertiesToIncludeOnUpdate =
                        [
                            nameof(MessageAttachment.HasBeenScanned),
                            nameof(MessageAttachment.IsSensitive),
                            nameof(MessageAttachment.ScannedAt)
                        ],
                        BatchSize = persistBatchSize
                    }, cancellationToken: ct);
                }
                if (matches.Any())
                {
                    await db.BulkInsertOrUpdateAsync(matches, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(MessageMatch.AttachmentId), nameof(MessageMatch.MessageId), nameof(MessageMatch.Pattern)],
                        PropertiesToExcludeOnUpdate = [
                            nameof(MessageMatch.MessageId)
                        ],
                        BatchSize = persistBatchSize
                    }, cancellationToken: ct);
                }
            },

            // 4) keySelector
            x => x.Attachment.Id,
            persistBatchSize:         persistBatchSize,       
            maxDegreeOfParallelism: Environment.ProcessorCount,
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