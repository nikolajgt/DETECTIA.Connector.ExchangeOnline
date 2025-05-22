using System.Text;
using System.Threading.Tasks.Dataflow;
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

        int totalMessages;
        await using (var ctx = await dbFactory.CreateDbContextAsync(cancellationToken))
            totalMessages = await ctx.Messages
                .Where(x => !x.HasBeenScanned)
                .CountAsync(cancellationToken);
        
        if(totalMessages < batchSize && totalMessages > 20)
            batchSize = (totalMessages / maxDegree) + 1;
        
        var totalPages = (int)Math.Ceiling(totalMessages / (double)batchSize);
        var pageBlock = new ActionBlock<int>(async pageIndex =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var batch = await db.Messages
                .Where(x => !x.HasBeenScanned)
                .Include(x => x.User)
                .OrderBy(m => m.Id)
                .Skip(pageIndex * batchSize)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var row in batch)
            {
                try
                {
                    var graphMsg = await graph
                        .Users[row.User.UserPrincipalName]
                        .Messages[row.GraphId]
                        .GetAsync(cfg =>
                        {
                            cfg.QueryParameters.Select = new[] { "subject", "body" };
                        }, cancellationToken);

                    var bodyText = graphMsg.Body?.Content;
                    if(string.IsNullOrEmpty(bodyText)) continue;
                    var response = await contentSearch.MatchAsync(bodyText);
                    if (response.Response.IsSensitive)
                        row.ContainSensitive = true;
                    row.HasBeenScanned = true;
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
                {
                    logger.LogWarning(
                        "Skipping scan for {User}/{Msg}: {Error}",
                        row.User.UserPrincipalName, row.GraphId, ex.Message);
                }
            }
            await db.SaveChangesAsync(cancellationToken);
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = maxDegree,
            BoundedCapacity        = maxDegree  
        });

        for (var i = 0; i < totalPages; i++)
            await pageBlock.SendAsync(i, cancellationToken);

        pageBlock.Complete();
        await pageBlock.Completion;
    }


   public async Task ScanEmailTextAsync(CancellationToken cancellationToken)
{
    var batchSize = 100;
    var maxDegree = Environment.ProcessorCount;

    await DataflowPipeline.RunAsync<EmailTextBatchItem, Message>(
        // ---- A) fetchPageAsync ----
        async (lastId, ct) =>
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(ct);
            return await ctx.Messages
                .AsNoTracking()
                .Where(m => !m.HasBeenScanned && m.Id > lastId)
                .OrderBy(m => m.Id)
                .Include(m => m.User)
                .Select(m => new EmailTextBatchItem(
                    m,
                    m.User.UserPrincipalName!
                ))
                .Take(batchSize)
                .ToListAsync(ct);
        },

        // ---- B) processBatchAsync ----
        async (batch, ct) =>
        {
            foreach (var item in batch)
            {
                try
                {
                    var graphMsg = await graph
                        .Users[item.UserPrincipalName]
                        .Messages[item.Message.GraphId]
                        .GetAsync(cfg => 
                            cfg.QueryParameters.Select = new[] { "subject", "body" },
                            ct
                        );

                    var body = graphMsg?.Body?.Content;
                    if (!string.IsNullOrEmpty(body))
                    {
                        var resp = await contentSearch.MatchAsync(body);
                        if (resp.Response.IsSensitive)
                            item.Message.ContainSensitive = true;
                    }
                    item.Message.HasBeenScanned = true;
                }
                catch (ODataError ex)
                {
                    logger.LogWarning(
                        "Skipping scan for {User}/{Msg}: {Error}",
                        item.UserPrincipalName, item.Message.GraphId, ex.Message
                    );
                }
            }

            // Return the actual Message entities for persistence
            return batch.Select(x => x.Message).ToList();
        },

        // ---- C) persistBatchAsync ----
        async (messages, ct) =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            await db.BulkUpdateAsync(messages, new BulkConfig
            {
                UpdateByProperties          = new[] { nameof(Message.Id) },
                PropertiesToIncludeOnUpdate = new[]
                {
                    nameof(Message.HasBeenScanned),
                    nameof(Message.ContainSensitive)
                },
                BatchSize = batchSize
            }, cancellationToken: ct);
        },

        // ---- D) keySelector for paging ----
        msg => msg.Id,

        // ---- E) other options ----
        fetchPageSize:     batchSize,
        groupSize:         1,                       // no grouping of processed batches
        maxDegreeOfParall: maxDegree,
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
        dbItem.ContainSensitive = await contentSearch.ContainsAsync(stream, attachmentContentType, Encoding.UTF8);
        dbItem.ScannedTime = DateTimeOffset.Now;
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

    private record AttachmentBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchCount { get; init; }
        internal MessageAttachment Attachment { get; init; }
        internal string MessageGraphId { get; init; }
        internal string UserPrincipalName { get; init; }
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
                        Attachment       = x,
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
                            .Attachments[item.Attachment.GraphId]
                            .GetAsync(cancellationToken: ct);
                        
                        if (attachment is null)
                        {
                            logger.LogWarning("Attachment returned was null for {item.MessageGraphId}", item.MessageGraphId);
                            continue;
                        }
                    
                        switch (attachment)
                        {
                            case FileAttachment fileAtt:
                                await HandleFileAttachmentAsync(fileAtt, item.Attachment, ct);
                                break;

                            case ItemAttachment itemAtt:
                                await HandleItemAttachmentAsync(itemAtt, item.Attachment, ct);
                                break;
                        
                            case ReferenceAttachment refAtt:
                                await HandleReferenceAttachmentAsync(refAtt, item.Attachment, ct);
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
                            item.UserPrincipalName, item.Attachment.GraphId, ex.Message);
                    }
                }
                logger.LogInformation("Finished scanning batch {}", batchNumber);
                return batch.Select(x => x.Attachment).ToList();
            },

            // 3) persistBatchAsync  (your save logic)
            async (attachments, ct) => {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                await db.BulkUpdateAsync(attachments, new BulkConfig
                {
                    UpdateByProperties          = [nameof(MessageAttachment.Id)],
                    PropertiesToIncludeOnUpdate =
                    [
                        nameof(MessageAttachment.HasBeenScanned),
                        nameof(MessageAttachment.ContainSensitive),
                        nameof(MessageAttachment.ScannedTime)
                    ]
                }, cancellationToken: ct);
            },

            // 4) keySelector
            x => x.Attachment.Id,

            fetchPageSize:     100,
            groupSize:         2,       
            maxDegreeOfParall: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
    }
}