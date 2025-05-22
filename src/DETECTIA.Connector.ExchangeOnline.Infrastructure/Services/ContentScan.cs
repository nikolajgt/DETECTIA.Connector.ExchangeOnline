using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using DETECTIA.ContentSearch.Application;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

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
        var batchSize = 100; 
        var maxDegree = Environment.ProcessorCount;

        int totalMessages;
        await using (var ctx = await dbFactory.CreateDbContextAsync(cancellationToken))
            totalMessages = await ctx.MessagesAttachements
                .Where(x => !x.HasBeenScanned)
                .CountAsync(cancellationToken);
        
        if(totalMessages < batchSize && totalMessages > 20)
            batchSize = (totalMessages / maxDegree) + 1;
        
        var totalPages = (int)Math.Ceiling(totalMessages / (double)batchSize);
        var pageBlock = new ActionBlock<int>(async pageIndex =>
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var batch = await db.MessagesAttachements
                .Where(x => !x.HasBeenScanned)
                .Include(x => x.Message)
                .ThenInclude(x => x.User)
                .Select(x => new
                {
                    Id = x.Id,
                    GraphId = x.GraphId,
                    UserPrincipalName = x.Message.User.UserPrincipalName,
                })
                .OrderBy(m => m.Id)
                .Skip(pageIndex * batchSize)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            foreach (var row in batch)
            {
                try
                {
                    var graphMsg = await graph
                        .Users[row.UserPrincipalName]
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
    
}