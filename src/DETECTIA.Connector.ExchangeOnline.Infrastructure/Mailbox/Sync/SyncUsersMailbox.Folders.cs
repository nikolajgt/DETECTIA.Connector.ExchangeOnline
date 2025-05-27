using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;


public partial class SyncUsersMailbox
{
    public async Task SyncUsersMailboxFoldersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
            var users = await dbContext.Users.ToListAsync(cancellationToken);
        
            var usersFolders = new List<UserMailFolder>();
            foreach (var user in users)
            {
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
                        var resp = await builder.GetAsDeltaGetResponseAsync(requestConfig =>
                        {
                            requestConfig.QueryParameters.Select =
                            [
                                "id",
                                "displayName",
                                "parentFolderId",
                                "childFolderCount",
                                "totalItemCount",
                                "unreadItemCount"
                            ];
                        }, cancellationToken);
                        if(resp is null) continue;

                        if (resp.Value is not null && resp.Value.Count > 0)
                        {
                            foreach (var f in resp.Value)
                            {
                                if (f.AdditionalData.TryGetValue("@removed", out _))
                                {
                                }
                                else
                                {
                                    var mapped = new UserMailFolder
                                    {
                                        GraphId              = f.Id!,
                                        UserId               = user.Id,
                                        DisplayName          = f.DisplayName!,
                                        ParentFolderId       = f.ParentFolderId,
                                        ChildFolderCount     = f.ChildFolderCount ?? 0,
                                        TotalItemCount       = f.TotalItemCount ?? 0,
                                        UnreadItemCount      = f.UnreadItemCount ?? 0,
                                        LastModifiedAt = DateTimeOffset.UtcNow,
                                        User                 = user
                                    };

                                    usersFolders.Add(mapped);
                                }
                            }
                        }

                        nextLink  = resp.OdataNextLink;
                        deltaLink = resp.OdataDeltaLink;

                        if (nextLink != null)
                            builder = builder.WithUrl(nextLink);
                    }
                    while (nextLink != null);
                    user.FoldersDeltaLink = deltaLink;
                }
                catch (ODataError ex)
                {
                    if (!ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase)
                        && !ex.Message.Contains("soft-deleted", StringComparison.OrdinalIgnoreCase)
                        && !ex.Message.Contains("on-premise", StringComparison.OrdinalIgnoreCase)) throw;
                    
                    logger.LogWarning(
                        "Skipping mailboxSettings for {Upn}: {Error}",
                        user.UserPrincipalName,
                        ex.Message);
                    continue;
                }
            }
            await dbContext.BulkInsertOrUpdateAsync(usersFolders, new BulkConfig
            {
                UpdateByProperties = [ nameof(UserMailFolder.GraphId) ],
                PropertiesToExcludeOnUpdate = [ nameof(UserMailFolder.GraphId), nameof(UserMailFolder.Id) ],
                SetOutputIdentity = false,
                PreserveInsertOrder = false,
                BatchSize = 500
            }, cancellationToken: cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }
}