using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using User = DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities.User;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;

public class SyncMetadata(ILogger<SyncMetadata> logger, GraphServiceClient graph, IDbContextFactory<AppDatabaseContext> dbFactory)
{
    public async Task SyncUsersAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var state = await dbContext.SyncStates.SingleOrDefaultAsync(s => s.Key == "UsersDelta", cancellationToken);
            var isNew = state == null;
            if (isNew)
            {
                state = new ExchangeSyncState { Key = "UsersDelta" };
                var entityResponse = await dbContext.SyncStates.AddAsync(state, cancellationToken);
                state = entityResponse.Entity;
            }

            var baseBuilder = graph.Users.Delta;
            var builder = string.IsNullOrEmpty(state.DeltaLink)
                ? baseBuilder
                : baseBuilder.WithUrl(state.DeltaLink);
            
            var users = new List<User>();
            string? nextLink = null, newDelta = null;
            do
            {
                var resp = await builder.GetAsDeltaGetResponseAsync(cfg =>
                {
                    cfg.QueryParameters.Select = new[]
                    {
                        "id",
                        "accountEnabled",
                        "displayName",
                        "givenName",
                        "surname",
                        "mail",
                        "userPrincipalName",
                        "mailNickname",
                        "jobTitle",
                        "department",
                        "officeLocation",
                        "mobilePhone",
                        "businessPhones",
                        "otherMails",
                        "onPremisesImmutableId",
                        "usageLocation",
                        "preferredLanguage",
                        "userType",
                        "createdDateTime",
                        "lastPasswordChangeDateTime"
                    };
                    cfg.QueryParameters.Top = 100;
                }, cancellationToken);

                foreach (var u in resp.Value)
                {
                    if (u.AdditionalData?.ContainsKey("@removed") == true)
                    {
                        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.ExchangeUserId == u.Id, cancellationToken);
                        if (user is null) continue;
                        user.AccountEnabled = false;
                        dbContext.Users.Update(user);
                    }
                    else
                    {
                        users.Add(new User
                        {
                            ExchangeUserId = u.Id!,
                            AccountEnabled = u.AccountEnabled ?? false,
                            DisplayName = u.DisplayName!,
                            GivenName = u.GivenName!,
                            Surname = u.Surname!,
                            Mail = u.Mail!,
                            UserPrincipalName = u.UserPrincipalName!,
                            MailNickname = u.MailNickname!,
                            JobTitle = u.JobTitle!,
                            Department = u.Department!,
                            OfficeLocation = u.OfficeLocation!,
                            MobilePhone = u.MobilePhone!,
                            BusinessPhones = u.BusinessPhones?.ToList() ?? [],
                            OtherMails = u.OtherMails?.ToList() ?? [],
                            OnPremisesImmutableId = u.OnPremisesImmutableId ?? string.Empty,
                            UsageLocation = u.UsageLocation!,
                            PreferredLanguage = u.PreferredLanguage!,
                            UserType = u.UserType!,
                            CreatedDateTime = u.CreatedDateTime,
                            LastPasswordChangeDateTime = u.LastPasswordChangeDateTime,
                            UserMailboxSettings = null, // these get fetched elsewhere
                            MailboxFolders = null, // these get fetched elsewhere
                            FoldersDeltaLink = null, // these get fetched elsewhere
                        });
                    }
                }

                nextLink = resp.OdataNextLink;
                newDelta = resp.OdataDeltaLink;
                if (nextLink != null)
                    builder = builder.WithUrl(nextLink);

            } while (nextLink != null);

            state.DeltaLink   = newDelta;
            state.LastSyncUtc = DateTimeOffset.UtcNow;
            
            await dbContext.BulkInsertOrUpdateAsync(users, new BulkConfig
            {
                UpdateByProperties = ["Id"],
                PropertiesToExcludeOnUpdate = ["Id", "CreatedDateTime"],
                SetOutputIdentity = false,
                PreserveInsertOrder = false,
                BatchSize = 500
            }, cancellationToken: cancellationToken);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SyncUsersAsync");
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

 
    public async Task SyncUsersMailboxSettingsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var users = await dbContext.Users.ToListAsync(cancellationToken);
        
        var mailboxSetting = new List<UserMailboxSettings>();
        foreach (var u in users)
        {
            try
            {
                var graphSettings = await graph
                    .Users[u.UserPrincipalName]
                    .MailboxSettings
                    .GetAsync(cfg =>
                    {
                        // pick only the fields you need
                        cfg.QueryParameters.Select = new[]
                        {
                            "archiveFolder",
                            "automaticRepliesSetting",
                            "dateFormat",
                            "timeFormat",
                            "timeZone",
                            "workingHours",
                            "delegateMeetingMessageDeliveryOptions"
                        };
                    }, cancellationToken);

                var mapped = new UserMailboxSettings
                {
                    ExchangeUserId = u.Id,
                    ArchiveFolder = graphSettings?.ArchiveFolder,
                    AutomaticRepliesEnabled =
                        graphSettings?.AutomaticRepliesSetting?.Status != AutomaticRepliesStatus.Disabled,
                    AutomaticRepliesInternalMessage = graphSettings?.AutomaticRepliesSetting?.InternalReplyMessage,
                    AutomaticRepliesExternalMessage = graphSettings?.AutomaticRepliesSetting?.ExternalReplyMessage,
                    DateFormat = graphSettings?.DateFormat,
                    TimeFormat = graphSettings?.TimeFormat,
                    TimeZone = graphSettings?.TimeZone,
                    // WorkingDays = graphSettings?.WorkingHours?.DaysOfWeek?
                    //     .Select(d => d?.ToString() ?? string.Empty)
                    //     .ToList(),
                    // WorkingHoursStartTime = graphSettings?.WorkingHours?.StartTime.Value.DateTime.TimeOfDay,
                    // WorkingHoursEndTime = graphSettings?.WorkingHours?.EndTime.Value.DateTime.TimeOfDay,
                    DelegateMeetingMessageDeliveryOptions = graphSettings?.DelegateMeetingMessageDeliveryOptions?.ToString()
                };
                mailboxSetting.Add(mapped);
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
            {
                // You can inspect ex.Error.Message or ex.Message here
                if (!ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase)
                    && !ex.Message.Contains("soft-deleted", StringComparison.OrdinalIgnoreCase)
                    && !ex.Message.Contains("on-premise", StringComparison.OrdinalIgnoreCase)) throw;
                
                logger.LogWarning(
                    "Skipping mailboxSettings for {Upn}: {Error}",
                    u.UserPrincipalName,
                    ex.Message);
                continue;
            }
        }
        
        await dbContext.BulkInsertOrUpdateAsync(mailboxSetting, new BulkConfig
        {
            UpdateByProperties = ["ExchangeUserId"],
            PropertiesToExcludeOnUpdate = ["ExchangeUserId"],
            SetOutputIdentity = false,
            PreserveInsertOrder = false,
            BatchSize = 500
        }, cancellationToken: cancellationToken);
    }

    

    public async Task SyncUsersFoldersAsync(CancellationToken cancellationToken)
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
                            requestConfig.QueryParameters.Top = 100;
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
                                        FolderId             = f.Id,
                                        DisplayName          = f.DisplayName!,
                                        ParentFolderId       = f.ParentFolderId,
                                        ChildFolderCount     = f.ChildFolderCount ?? 0,
                                        TotalItemCount       = f.TotalItemCount ?? 0,
                                        UnreadItemCount      = f.UnreadItemCount ?? 0,
                                        LastModifiedDateTime = DateTimeOffset.UtcNow,
                                        ExchangeUserId       = user.Id,
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
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
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
                UpdateByProperties = ["ExchangeUserId"],
                PropertiesToExcludeOnUpdate = ["ExchangeUserId, Id"],
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
    
    public async Task SyncAllUsersMessagesAsync(CancellationToken cancellationToken)
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
                        .MailFolders[folder.FolderId]   
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
                                "parentFolderId"
                            };
                            cfg.QueryParameters.Top = 100;
                        }, cancellationToken);

                        foreach (var m in page.Value)
                        {
                            if (!m.AdditionalData.ContainsKey("@removed"))
                            {
                                // userMessages.Add(new UserMessage
                                // {
                                //     MessageId         = m.Id!,
                                //     FolderId          = folder.Id,        
                                //     Subject           = m.Subject,
                                //     From              = m.From?.EmailAddress?.Address,
                                //     ReceivedDateTime  = m.ReceivedDateTime ?? default,
                                //     IsRead            = m.IsRead ?? false,
                                // });
                            }
                        }

                        nextMsgLink   = page.OdataNextLink;
                        newMsgDelta   = page.OdataDeltaLink;
                        if (nextMsgLink != null)
                            msgBuilder = msgBuilder.WithUrl(nextMsgLink);

                    } while (nextMsgLink != null);

                    folder.MessagesDeltaLink = newMsgDelta;
                    dbContext.MailFolders.Update(folder);
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
                nameof(UserMessage.MessageId),
            ],
            PropertiesToExcludeOnUpdate = 
            [
                nameof(UserMessage.Id),
                nameof(UserMessage.MessageId),
                nameof(UserMessage.FolderId)
            ],
            SetOutputIdentity   = false,
            PreserveInsertOrder = false,
            BatchSize           = 500
        }, cancellationToken: cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}