using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Scan;

public partial class ScanUsersMailbox
{
    private sealed record MessageProcess(UserMessage Message, string UserPrincipalName);
    
    public async Task ScanUsersMessageTextAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        var maxDegree = Environment.ProcessorCount;
    
        await DataflowScanPipeline.RunAsync<MessageProcess, UserMessage, MessageMatch>(
            async (lastId, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                return await ctx.UserMessages
                    .AsNoTracking()
                    .Where(m => !m.HasBeenScanned && m.Id > lastId)
                    .OrderBy(m => m.Id)
                    .Include(m => m.User)
                    .Select(m => new MessageProcess(m, m!.User!.UserPrincipalName!))
                    .Take(dbFetchPageSize)
                    .ToListAsync(ct);
            },
            async (item, ct) =>
            {
                var matches = new List<MessageMatch>();
                try
                {
                    var graphMsg = await graph
                        .Users[item.UserPrincipalName]
                        .Messages[item.Message.GraphId]
                        .GetAsync(cfg => 
                                cfg.QueryParameters.Select = ["subject", "body"],
                            ct
                        );
    
                    var body = graphMsg?.Body?.Content ?? string.Empty;
                    var resp = await contentSearch.MatchAsync(body);
                    var convertedMatches = resp.Response.Matches.GroupBy(obj => new { obj.Name, obj.Pattern })
                        .Select(group => new MessageMatch
                        {
                            Name = group.Key.Name,
                            Pattern = group.Key.Pattern,
                            MatchCount = group.Count(),
                            MessageId = item.Message.Id
                        }).ToList();
                    matches.AddRange(convertedMatches);
                    item.Message.IsSensitive = resp.Response.IsSensitive;
                    item.Message.ScannedAt = DateTimeOffset.UtcNow;
                }
                catch (ODataError ex)
                {
                    logger.LogWarning(
                        "Skipping scan for {User}/{Msg}: {Error}",
                        item.UserPrincipalName, item.Message.GraphId, ex.Message
                    );
                }

                return (item.Message, matches);
            },
            async (messages, matches, ct) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                if (messages.Any())
                {
                    await db.BulkUpdateAsync(messages, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(UserMessage.Id)] ,
                        PropertiesToIncludeOnUpdate =
                        [
                            nameof(UserMessage.HasBeenScanned),
                            nameof(UserMessage.IsSensitive),
                            nameof(UserMessage.ScannedAt)
                        ],
                        BatchSize = persistBatchSize
                    }, cancellationToken: ct);
                }
                if (matches.Any())
                {
                    await db.BulkInsertOrUpdateAsync(matches, new BulkConfig
                    {
                        UpdateByProperties          = [nameof(MessageMatch.MessageId), nameof(MessageMatch.Pattern)],
                        PropertiesToExcludeOnUpdate = [
                            nameof(MessageMatch.AttachmentId)
                        ],
                        BatchSize = persistBatchSize
                    }, cancellationToken: ct);
                }
            },
    
            msg => msg.Message.Id,
            persistBatchSize: persistBatchSize,                       
            maxDegreeOfParallelism: maxDegree,
            cancellationToken: cancellationToken
        );
    }
}