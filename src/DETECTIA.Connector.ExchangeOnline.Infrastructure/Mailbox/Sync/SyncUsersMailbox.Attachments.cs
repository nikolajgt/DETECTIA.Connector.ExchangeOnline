using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;

public partial class SyncUsersMailbox
{
    public record MessageInfo(string UserPrincipalName, string GraphId, long MessageId);
    public async Task SyncUsersMessageAttachmentsAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        await DataflowSyncPipeline.RunAsync<MessageInfo, MessageAttachment>(
            fetchPageAsync: async (lastKey, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
                return await dbContext.UserMessages
                    .AsNoTracking()
                    .Where(m => m.Id > lastKey)
                    .OrderBy(m => m.Id)
                    .Include(m => m.User)
                    .Select(m => new MessageInfo(m.User!.UserPrincipalName!, m.GraphId, m.Id))
                    .Take(100)
                    .ToListAsync(ct);
            },
            expandAsync: async (message, ct) =>
            {
                var attachments = await graph.Users[message.UserPrincipalName]
                    .Messages[message.GraphId]
                    .Attachments
                    .GetAsync(cfg => {
                        cfg.QueryParameters.Select =
                        [
                            "id",
                            "name",
                            "contentType",
                            "size",
                            "isInline",
                            "lastModifiedDateTime"
                        ];
                    }, ct);

                return attachments?.Value?.Select(att => new MessageAttachment
                {
                    Id                    = 0,              
                    MessageId             = message.MessageId,    
                    GraphId               = att.Id!,
                    Name                  = att.Name!,
                    ContentType           = att.ContentType!,
                    Size                  = att.Size ?? 0,
                    IsInline              = att.IsInline ?? false,
                    LastModifiedAt        = att.LastModifiedDateTime,
                    HasBeenScanned        = false,
                }) ?? [];
            },
            persistBatchAsync: async (batch, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                await ctx.BulkInsertOrUpdateAsync(batch, new BulkConfig
                {
                    UpdateByProperties = [
            
                        nameof(MessageAttachment.GraphId)
                    ],
                    PropertiesToExcludeOnUpdate = [
                        nameof(MessageAttachment.Id),
                        nameof(MessageAttachment.MessageId)
                    ],
                    SetOutputIdentity   = false,
                    PreserveInsertOrder = false,
                    BatchSize           = 500
                }, cancellationToken: ct);
            },
            keySelector: message => message.MessageId,
            persistBatchSize: 500,
            maxDegreeOfParallelism: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
    }
}