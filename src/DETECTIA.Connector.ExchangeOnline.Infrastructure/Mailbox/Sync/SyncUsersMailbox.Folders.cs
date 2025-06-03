using System.Collections.Concurrent;
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
    public async Task SyncUsersMailboxFoldersAsync(CancellationToken cancellationToken)
    {
        const int dbFetchPageSize = 100;
        const int persistBatchSize = 500;
        await DataflowSyncPipeline.RunAsync<User, UserMailFolder, User>(
            fetchPageAsync: async (lastUserId, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
                return await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id > lastUserId)
                    .OrderBy(u => u.Id)
                    .Take(dbFetchPageSize)
                    .ToListAsync(ct);
            },
            expandAsync: async (user, ct) =>
            {
                var userFolders = new List<UserMailFolder>();
                try
                {
                    var baseBuilder = graph
                        .Users[user.UserPrincipalName]
                        .MailFolders
                        .Delta;
                    var builder = string.IsNullOrEmpty(user.FoldersDeltaLink)
                        ? baseBuilder
                        : baseBuilder.WithUrl(user.FoldersDeltaLink);

                    string? nextLink = null;
                    string? deltaLink = null;

                    do
                    {
                        var resp = await builder.GetAsDeltaGetResponseAsync(cfg =>
                        {
                            cfg.QueryParameters.Select =
                            [
                                "id",
                                "displayName",
                                "parentFolderId",
                                "childFolderCount",
                                "totalItemCount",
                                "unreadItemCount"
                            ];
                        }, ct);

                        if (resp?.Value is not null)
                        {
                            foreach (var f in resp.Value)
                            {
                                if (!f.AdditionalData.TryGetValue("@removed", out _))
                                {
                                    userFolders.Add(new UserMailFolder
                                    {
                                        GraphId              = f.Id!,
                                        UserId               = user.Id,
                                        DisplayName          = f.DisplayName!,
                                        ParentFolderId       = f.ParentFolderId,
                                        ChildFolderCount     = f.ChildFolderCount ?? 0,
                                        TotalItemCount       = f.TotalItemCount ?? 0,
                                        UnreadItemCount      = f.UnreadItemCount ?? 0,
                                        LastSyncUtc          = DateTimeOffset.UtcNow,
                                        User                 = user
                                    });
                                }
                                else
                                {
                                    // deleted
                                }
                            }
                        }

                        nextLink = resp?.OdataNextLink;
                        deltaLink = resp?.OdataDeltaLink;

                        if (nextLink != null)
                            builder = builder.WithUrl(nextLink);
                    }
                    while (nextLink != null);

                    user.FoldersDeltaLink = deltaLink;
                }
                catch (ODataError ex)
                {
                    if (!ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase) &&
                        !ex.Message.Contains("soft-deleted", StringComparison.OrdinalIgnoreCase) &&
                        !ex.Message.Contains("on-premise", StringComparison.OrdinalIgnoreCase))
                    {
                        throw;
                    }

                    logger.LogWarning("Skipping mailbox folders for {Upn}: {Error}", user.UserPrincipalName, ex.Message);
                }
                return (userFolders, user);
            },
            persistBatchAsync: async (folders, users, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
                await dbContext.BulkInsertOrUpdateAsync(folders, new BulkConfig
                {
                    UpdateByProperties = [nameof(UserMailFolder.GraphId)],
                    PropertiesToExcludeOnUpdate = [nameof(UserMailFolder.Id)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = persistBatchSize
                }, cancellationToken: ct);
                
                await dbContext.BulkInsertOrUpdateAsync(users, new BulkConfig
                {
                    UpdateByProperties = [nameof(UserMailFolder.GraphId)],
                    PropertiesToExcludeOnUpdate = [nameof(UserMailFolder.Id)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = persistBatchSize
                }, cancellationToken: ct);
            },
            keySelector: u => u.Id,
            persistBatchSize: persistBatchSize, 
            maxDegreeOfParallelism: Environment.ProcessorCount,
            cancellationToken: cancellationToken
        );
    }
}