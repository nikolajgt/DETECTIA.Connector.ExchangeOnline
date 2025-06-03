using System.Collections.Concurrent;
using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;


public partial class SyncUsersMailbox
{
    public readonly struct PipelineProcess(IEnumerable<UserMailFolder> Folders, User users);

    public async Task SyncUsersMailboxFoldersAsync(CancellationToken cancellationToken)
    {
        const int batchSize = 500;
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var userBag = new ConcurrentQueue<User>();
        await DataflowSyncPipeline.RunAsync<User, UserMailFolder>(
            fetchPageAsync: async (lastUserId, ct) =>
            {
                return await dbContext.Users
                    .Where(u => u.Id > lastUserId)
                    .OrderBy(u => u.Id)
                    .Take(100)
                    .ToListAsync(ct);
            },
            expandAsync: async (user, ct) =>
            {
                var userFolders = new List<UserMailFolder>();
                try
                {
                    var baseBuilder = graph.Users[user.UserPrincipalName].MailFolders.Delta;
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
                userBag.Enqueue(user);
                return userFolders;
            },
            persistBatchAsync: async (batch, ct) =>
            {
                await using var ctx = await dbFactory.CreateDbContextAsync(ct);
                // fx folders reached max batchsize fx 100
                await ctx.BulkInsertOrUpdateAsync(batch, new BulkConfig
                {
                    UpdateByProperties = [nameof(UserMailFolder.GraphId)],
                    PropertiesToExcludeOnUpdate = [nameof(UserMailFolder.Id)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = batchSize
                }, cancellationToken: ct);
                
                var users = new List<User>();
                while (userBag.TryDequeue(out var user) && users.Count < batchSize)
                    users.Add(user);
                
                await ctx.BulkInsertOrUpdateAsync(users, new BulkConfig
                {
                    UpdateByProperties = [nameof(User.Id)],
                    PropertiesToIncludeOnUpdate = [nameof(User.FoldersDeltaLink)],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = batchSize
                }, cancellationToken: ct);
            },
            keySelector: u => u.Id,
            batchSize: 100, // Adjust as needed
            maxDegreeOfParallelism: 4,
            cancellationToken: cancellationToken
        );
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}