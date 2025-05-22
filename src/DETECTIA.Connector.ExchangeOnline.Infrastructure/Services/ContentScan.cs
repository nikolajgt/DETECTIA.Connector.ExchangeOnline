using System.Text;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using DETECTIA.ContentSearch.Application;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;

public class ContentScan(
    IContentSearch contentSearch, 
    ILogger<ContentScan> logger, 
    GraphServiceClient graph, 
    IDbContextFactory<AppDatabaseContext> dbFactory)
{

   public async Task ScanEmailTextAsync(CancellationToken cancellationToken)
    {
        var batchSize = 100;
        var maxDegree = Environment.ProcessorCount;
    
        await DataflowPipeline.RunAsync<MessageBatch, UserMessage>(
            // ---- A) fetchPageAsync ----
            async (lastId, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.Messages
                    .AsNoTracking()
                    .Where(m => !m.HasBeenScanned && m.Id > lastId)
                    .OrderBy(m => m.Id)
                    .Include(m => m.User)
                    .Select(m => new MessageBatch
                    {
                        Entity            = m,
                        UserPrincipalName= m.User!.UserPrincipalName!
                    })
                    .Take(batchSize)
                    .ToListAsync(ct);
            },
    
            async (batch, ct) =>
            {
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
                        var resp = await contentSearch.MatchAsync(body);
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
                return batch.Select(x => x.Entity).ToList();
            },
    
            async (messages, ct) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                await db.BulkUpdateAsync(messages, new BulkConfig
                {
                    UpdateByProperties          = [nameof(UserMessage.Id)] ,
                    PropertiesToIncludeOnUpdate = [
                        nameof(UserMessage.IsSensitive),
                        nameof(UserMessage.ScannedAt)
                    ],
                    BatchSize = batchSize
                }, cancellationToken: ct);
            },
    
            // ---- D) keySelector for paging ----
            msg => msg.Entity.Id,
    
            // ---- E) other options ----
            fetchPageSize:     batchSize,
            groupSize:         1,                       
            maxDegreeOfParall: maxDegree,
            cancellationToken: cancellationToken
        );
    }

    public async Task ScanEmailAttachmentsAsync(CancellationToken cancellationToken)
    {
        var batchSize = 100;
        
        await DataflowPipeline.RunAsync<AttachmentBatch, MessageAttachment>(
            async (lastId, ct) => {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.MessagesAttachements
                    .AsNoTracking()
                    .Where(x => !x.HasBeenScanned && x.Id > lastId)
                    .OrderBy(x => x.Id)
                    .Include(x => x.Message)
                        .ThenInclude(m => m.User)
                    .Select(x => new AttachmentBatch {
                        Entity       = x,
                        MessageGraphId   = x.Message!.GraphId,
                        UserPrincipalName= x.Message.User.UserPrincipalName!
                    })
                    .Take(batchSize)
                    .ToListAsync(ct);
            },

            async (batch, ct) => {
                var batchNumber = batch.First().BatchNumber;
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
                            logger.LogWarning("Attachment returned was null for {item.MessageGraphId}", item.MessageGraphId);
                            continue;
                        }
                    
                        switch (attachment)
                        {
                            case FileAttachment fileAtt:
                                await HandleFileAttachmentAsync(fileAtt, item.Entity, ct);
                                break;

                            case ItemAttachment itemAtt:
                                await HandleItemAttachmentAsync(itemAtt, item.Entity, ct);
                                break;
                        
                            case ReferenceAttachment refAtt:
                                await HandleReferenceAttachmentAsync(refAtt, item.Entity, ct);
                                break;

                            default:
                                logger.LogWarning("Attachment {Id} is not a FileAttachment or ItemAttachment", attachment.Id);
                                break;
                        }
                    }
                    catch (ODataError ex)
                    {
                        logger.LogWarning(
                            "Skipping scan for {User}/{Msg}: {Error}",
                            item.UserPrincipalName, item.Entity.GraphId, ex.Message);
                    }
                }
                logger.LogInformation("Finished scanning batch {}", batchNumber);
                return batch.Select(x => x.Entity).ToList();
            },

            async (attachments, ct) => {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                await db.BulkUpdateAsync(attachments, new BulkConfig
                {
                    UpdateByProperties          = [nameof(MessageAttachment.Id)],
                    PropertiesToIncludeOnUpdate =
                    [
                        nameof(MessageAttachment.HasBeenScanned),
                        nameof(MessageAttachment.IsSensitive),
                        nameof(MessageAttachment.ScannedAt)
                    ]
                }, cancellationToken: ct);
            },

            // 4) keySelector
            x => x.Entity.Id,

            fetchPageSize:     100,
            groupSize:         2,       
            maxDegreeOfParall: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
    }
    
    private async Task HandleFileAttachmentAsync(
        FileAttachment fileAttachment, 
        MessageAttachment dbItem,
        CancellationToken cancellationToken)
    {
        if(fileAttachment.ContentBytes is null)
        {
            logger.LogWarning("Teeeeeeeeeest");
            return;
        }

        if (!ContentSearch.Infrastructure.Helpers.ContentTypeAndExtensionHelper.GetContentType(
                Path.GetExtension(fileAttachment.Name).TrimStart('.'), out var attachmentContentType)) return;
                    
        await using var stream = new MemoryStream(fileAttachment.ContentBytes);
        dbItem.IsSensitive = await contentSearch.ContainsAsync(stream, attachmentContentType, Encoding.UTF8);
        dbItem.ScannedAt = DateTimeOffset.Now;
        dbItem.HasBeenScanned = true;
    }

    private async Task HandleItemAttachmentAsync(
        ItemAttachment itemAttachment, 
        MessageAttachment dbItem,
        CancellationToken cancellationToken)
    {
        
    }

    private async Task HandleReferenceAttachmentAsync(
        ReferenceAttachment referenceAttachment,
        MessageAttachment dbItem,
        CancellationToken cancellationToken)
    {
        
    }

    
    private record MessageBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchCount { get; init; }
        internal required UserMessage Entity { get; init; }
        internal required string UserPrincipalName { get; init; }
    }
    
    private record AttachmentBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchCount { get; init; }
        internal required MessageAttachment Entity { get; init; }
        internal required string MessageGraphId { get; init; }
        internal required string UserPrincipalName { get; init; }
    }
}