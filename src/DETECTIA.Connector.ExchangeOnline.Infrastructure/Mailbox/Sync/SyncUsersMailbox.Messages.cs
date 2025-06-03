using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;


public partial class SyncUsersMailbox
{
    public async Task SyncUsersMessagesAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        await DataflowSyncPipeline.RunAsync<UserMailFolder, UserMessage, UserMailFolder>(
            fetchPageAsync: async (lastFolderId, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
                return await dbContext.UserMailFolders
                    .AsNoTracking()
                    .Include(f => f.User)
                    .Where(f => f.Id > lastFolderId)
                    .OrderBy(f => f.Id)
                    .Take(dbFetchPageSize)
                    .ToListAsync(ct);
            },
            expandAsync: async (folder, ct) =>
            {
                var user = folder.User!;
                var userMessages = new List<UserMessage>();

                try
                {
                    var baseMsgBuilder = graph
                        .Users[user.UserPrincipalName]
                        .MailFolders[folder.GraphId]
                        .Messages
                        .Delta;

                    var msgBuilder = string.IsNullOrEmpty(folder.MessagesDeltaLink)
                        ? baseMsgBuilder
                        : baseMsgBuilder.WithUrl(folder.MessagesDeltaLink);

                    string? nextMsgLink, newMsgDelta;
                    do
                    {
                        var page = await msgBuilder.GetAsDeltaGetResponseAsync(cfg =>
                        {
                            cfg.QueryParameters.Select =
                            [
                                "id",
                                "subject",
                                "from",
                                "receivedDateTime",
                                "isRead",
                                "parentFolderId",
                                "internetMessageId"
                            ];
                            cfg.QueryParameters.Top = 100;
                        }, ct) ?? new();

                        foreach (var m in page?.Value)
                        {
                            if (!m.AdditionalData.ContainsKey("@removed"))
                            {
                                userMessages.Add(new UserMessage
                                {
                                    GraphId           = m.Id!,
                                    FolderId          = folder.Id,
                                    UserId            = folder.UserId,
                                    Subject           = m.Subject,
                                    From              = m.From?.EmailAddress?.Address,
                                    ReceivedAt        = m.ReceivedDateTime ?? default,
                                    IsRead            = m.IsRead ?? false,
                                    InternetMessageId = m.InternetMessageId!,
                                    HasBeenScanned    = false,
                                });
                            }
                            // else: consider handling deletes
                        }

                        nextMsgLink = page.OdataNextLink;
                        newMsgDelta = page.OdataDeltaLink;

                        if (nextMsgLink != null)
                            msgBuilder = msgBuilder.WithUrl(nextMsgLink);

                    } while (nextMsgLink != null);

                    folder.MessagesDeltaLink = newMsgDelta;
                }
                catch (ODataError ex) when (ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Skipping messages-delta for folder {Folder} of {Upn}: {Err}",
                        folder.DisplayName, user.UserPrincipalName, ex.Message);
                }

                return (userMessages, folder);
            },
            persistBatchAsync: async (messages, folder, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                await ctx.BulkInsertOrUpdateAsync(messages, new BulkConfig
                {
                    UpdateByProperties = [ nameof(UserMessage.GraphId) ],
                    PropertiesToExcludeOnUpdate = [ nameof(UserMessage.Id), nameof(UserMessage.GraphId) ],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = persistBatchSize
                }, cancellationToken: ct);
                
                await ctx.BulkInsertOrUpdateAsync(folder, new BulkConfig
                {
                    UpdateByProperties = [ nameof(UserMailFolder.Id) ],
                    PropertiesToInclude = [ nameof(UserMailFolder.MessagesDeltaLink)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = persistBatchSize
                }, cancellationToken: ct);
            },
            keySelector: f => f.Id,
            persistBatchSize: persistBatchSize,
            maxDegreeOfParallelism: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
    }
}