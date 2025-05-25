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

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services.ContentSearchScan;



public class MessageAttachmentScan(
    IContentSearch contentSearch, 
    ILogger<MessageAttachmentScan> logger, 
    GraphServiceClient graph, 
    IDbContextFactory<AppDatabaseContext> dbFactory)
{
    private record AttachmentBatch
    {
        internal int BatchNumber { get; init; }
        internal int TotalBatchCount { get; init; }
        internal required UserMessageAttachment Entity { get; init; }
        internal required string MessageGraphId { get; init; }
        internal required string UserPrincipalName { get; init; }
    }
    
    
     public async Task ScanEmailAttachmentsAsync(CancellationToken cancellationToken)
    {
        var batchSize = 10;
        
        await DataflowPipeline.RunAsync<AttachmentBatch, UserMessageAttachment>(
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
                var matches = new List<Match>();
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
                return new DataflowPipeline.PipelineScanProcess<UserMessageAttachment>
                {
                    Entities = batch.Select(x => x.Entity).ToList(),
                    Matches = matches
                };
            },

            async (attachments, ct) => {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (attachments.Entities.Any())
                {
                    await db.BulkUpdateAsync(attachments.Entities, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(UserMessageAttachment.Id)] ,
                        PropertiesToIncludeOnUpdate =
                        [
                            nameof(UserMessageAttachment.HasBeenScanned),
                            nameof(UserMessageAttachment.IsSensitive),
                            nameof(UserMessageAttachment.ScannedAt)
                        ],
                        BatchSize = batchSize
                    }, cancellationToken: ct);
                }
                if (attachments.Matches.Any())
                {
                    await db.BulkInsertOrUpdateAsync(attachments.Matches, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(Match.AttachmentId), nameof(Match.Pattern)],
                        PropertiesToExcludeOnUpdate = [
                            nameof(Match.MessageId)
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
    
    
    private async Task<List<Match>> HandleFileAttachmentAsync(
        FileAttachment fileAttachment, 
        UserMessageAttachment dbItem,
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
            .Select(group => new Match
            {
                Name = group.Key.Name,
                Pattern = group.Key.Pattern,
                MatchCount = group.Count(),
                AttachmentId = dbItem.Id
            }).ToList();
    }

    private Task<List<Match>> HandleItemAttachmentAsync(
        ItemAttachment itemAttachment, 
        UserMessageAttachment dbItem,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<Match>());
    }

    private Task<List<Match>> HandleReferenceAttachmentAsync(
        ReferenceAttachment referenceAttachment,
        UserMessageAttachment dbItem,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<Match>());
    }
}