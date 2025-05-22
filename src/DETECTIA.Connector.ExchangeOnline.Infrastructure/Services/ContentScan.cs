using System.Text;
using System.Threading.Channels;
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
using User = Microsoft.Graph.Models.User;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items       { get; init; } = [];
    public int              PageNumber  { get; init; }
    public int              PageSize    { get; init; }
    public int              TotalCount  { get; init; }
    public int              TotalPages  => (int)Math.Ceiling(TotalCount / (double)PageSize);
}


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


    public async Task ScanEmailAttachmentsAsync(CancellationToken cancellationToken)
    {
        var batchSize  = 100;
        var maxDegree  = Environment.ProcessorCount;

        var scanBlock = new TransformBlock<List<AttachmentBatch>, List<MessageAttachment>>(async batch => 
        {
            var batchNumber = batch.First().BatchNumber;
            foreach (var item in batch)
            {
                try
                {
                    var attachment = await graph
                        .Users[item.UserPrincipalName]
                        .Messages[item.MessageGraphId]
                        .Attachments[item.Attachment.GraphId]
                        .GetAsync(cfg => {
                                cfg.QueryParameters.Select = new[] {
                                    "name",
                                    "contentType",
                                    "contentBytes"
                                };
                            },
                            cancellationToken);
                    
                    if(attachment == null || attachment is not FileAttachment fileAttachment) continue;
                    
                    if(fileAttachment.ContentBytes is null) continue;
                    
                    if (ContentSearch.Infrastructure.Helpers.ContentTypeAndExtensionHelper.GetContentType(
                            Path.GetExtension(attachment.Name).TrimStart('.'), out var attachmentContentType))
                    {
                        using var stream = new MemoryStream(fileAttachment.ContentBytes);
                        var isSensitive = await contentSearch.ContainsAsync(stream, attachmentContentType, Encoding.UTF8);
                        item.Attachment.ScannedTime = DateTimeOffset.Now;
                        item.Attachment.HasBeenScanned = true;
                        if (isSensitive)
                        {
                            logger.LogError("Sensitive something found");
                            item.Attachment.ContainSensitive = true;
                        }
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
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = maxDegree,
            BoundedCapacity        = maxDegree,
            CancellationToken      = cancellationToken,
            EnsureOrdered          = false,
        });
        
        
        var batchOf2 = new BatchBlock<List<MessageAttachment>>(2);
        var flatten = new TransformBlock<List<MessageAttachment>[], List<MessageAttachment>>(
            batches => batches.SelectMany(batch => batch).ToList(),
            new ExecutionDataflowBlockOptions {
                BoundedCapacity   = maxDegree,
                CancellationToken = cancellationToken
            });
        
        var persistBlock = new ActionBlock<List<MessageAttachment>>(
            async batch =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
                await db.BulkInsertOrUpdateAsync(batch, new BulkConfig
                {
                    UpdateByProperties          = [nameof(MessageAttachment.Id)],
                    PropertiesToIncludeOnUpdate =
                    [
                        nameof(MessageAttachment.HasBeenScanned),
                        nameof(MessageAttachment.ContainSensitive)
                    ],
                    BatchSize = batchSize
                }, cancellationToken: cancellationToken);
                
                logger.LogInformation("Persisted batch with {} elements", batch.Count);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                BoundedCapacity        = maxDegree,
                CancellationToken      = cancellationToken,
                EnsureOrdered          = false
            }
        );
        
        scanBlock.LinkTo(batchOf2,    new DataflowLinkOptions { PropagateCompletion = true });
        batchOf2.LinkTo(flatten,      new DataflowLinkOptions { PropagateCompletion = true });
        flatten.LinkTo(persistBlock,  new DataflowLinkOptions { PropagateCompletion = true });

        long lastId = 0;
        int batchNumber = 1;
        while (true)
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(cancellationToken);
            var batch = await ctx.MessagesAttachements
                .AsNoTracking()
                .Where(x => !x.HasBeenScanned && x.Message.Id > lastId)
                .Include(x => x.Message)
                .ThenInclude(x => x.User)
                .OrderBy(x => x.Message.Id)
                .Select(x => new AttachmentBatch
                {
                    BatchNumber = batchNumber,
                    TotalBatchNumber = 999,
                    MessageGraphId = x.Message!.GraphId,
                    Attachment = x,
                    UserPrincipalName = x.Message.User.UserPrincipalName!,
                })
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
                break;

            lastId = batch.Last().Attachment.Id;

            await scanBlock.SendAsync(item: batch, cancellationToken: cancellationToken);
            batchNumber++;
        }

        scanBlock.Complete();
        await scanBlock.Completion;
    }

    private record AttachmentBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchNumber { get; init; }
        internal MessageAttachment Attachment { get; init; }
        internal string MessageGraphId { get; init; }
        internal string UserPrincipalName { get; init; }
    }
}