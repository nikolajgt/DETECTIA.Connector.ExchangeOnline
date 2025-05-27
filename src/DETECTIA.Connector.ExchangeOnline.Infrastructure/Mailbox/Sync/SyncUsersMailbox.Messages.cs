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
    public async Task SyncUsersMessagesAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var users = await dbContext.Users
            .Where(u => u.AccountEnabled)
            .Include(x => x.MailboxFolders)
            .ToListAsync(cancellationToken);

        var userMessages = new List<UserMessage>();
        foreach (var user in users)
        {
            foreach (var folder in user.MailboxFolders!)
            {
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
                            cfg.QueryParameters.Select = new[]
                            {
                                "id",
                                "subject",
                                "from",
                                "receivedDateTime",
                                "isRead",
                                "parentFolderId",
                                "internetMessageId"
                            };
                            cfg.QueryParameters.Top = 100;
                        }, cancellationToken) ?? new();
                        
                        
                        foreach (var m in page.Value)
                        {
                            if (m.AdditionalData.ContainsKey("@removed"))
                            {
                                // set object to removed/deleted
                            }
                            else
                            {
                                userMessages.Add(new UserMessage
                                {
                                    GraphId           = m.Id!,
                                    FolderId          = folder.Id,        
                                    UserId            = user.Id,
                                    Subject           = m.Subject,
                                    From              = m.From?.EmailAddress?.Address,
                                    ReceivedAt        = m.ReceivedDateTime ?? default,
                                    IsRead            = m.IsRead ?? false,
                                    InternetMessageId = m.InternetMessageId!,
                                    HasBeenScanned    = false,
                                });
                            }
                        }

                        nextMsgLink   = page.OdataNextLink;
                        newMsgDelta   = page.OdataDeltaLink;
                        if (nextMsgLink != null)
                            msgBuilder = msgBuilder.WithUrl(nextMsgLink);

                    } while (nextMsgLink != null);

                    folder.MessagesDeltaLink = newMsgDelta;
                    dbContext.UserMailFolders.Update(folder);
                }
                catch (ODataError ex) when (ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Skipping messages‐delta for folder {Folder} of {Upn}: {Err}", 
                        folder.DisplayName, user.UserPrincipalName, ex.Message);
                }
            }
        }

        // 5) Bulk upsert all messages
        await dbContext.BulkInsertOrUpdateAsync(userMessages, new BulkConfig
        {
            UpdateByProperties = 
            [
                nameof(UserMessage.GraphId),
            ],
            PropertiesToExcludeOnUpdate = 
            [
                nameof(UserMessage.Id),
                nameof(UserMessage.GraphId)
            ],
            SetOutputIdentity   = false,
            PreserveInsertOrder = false,
            BatchSize           = 500
        }, cancellationToken: cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}