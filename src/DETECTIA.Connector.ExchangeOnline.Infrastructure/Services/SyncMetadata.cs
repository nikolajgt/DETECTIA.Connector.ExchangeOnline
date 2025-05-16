using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;

public class SyncMetadata(ILogger<SyncMetadata> logger, GraphServiceClient graph, IDbContextFactory<AppDatabaseContext> dbFactory)
{
    public async Task SyncUsersAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var state = await dbContext.SyncStates
                .SingleOrDefaultAsync(s => s.Key == "UsersDelta", cancellationToken);
            if (state == null)
            {
                state = new ExchangeUsersSyncState { Key = "UsersDelta" };
                dbContext.SyncStates.Add(state);
            }

            var baseBuilder = graph.Users.Delta;
            var builder = string.IsNullOrEmpty(state.DeltaLink)
                ? baseBuilder
                : baseBuilder.WithUrl(state.DeltaLink);

            var existingUsers = await dbContext.Users
                .Where(u => u.AccountEnabled)
                    .Include(x => x.MailboxFolders)
                    .Include(x => x.UserMailboxSettings)
                .ToListAsync(cancellationToken);
            
            var byId = existingUsers
                .ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);

            string? nextLink = null, newDelta = null;
            do
            {
                var resp = await builder.GetAsDeltaGetResponseAsync(cfg =>
                {
                    //cfg.QueryParameters.Filter = "mail ne null";
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
                    // 5a) Deleted?
                    if (u.AdditionalData?.ContainsKey("@removed") == true)
                    {
                        if (byId.TryGetValue(u.Id!, out var toRemove))
                        {
                            toRemove.AccountEnabled = false;
                            dbContext.Users.Update(toRemove);
                            byId.Remove(u.Id!);
                        }
                    }
                    else
                    {
                        var existingUser = existingUsers?.SingleOrDefault(x => x.Id == u.Id);
                        var mapped = new ExchangeUser
                        {
                            Id                        = u.Id!,
                            AccountEnabled            = u.AccountEnabled ?? false,
                            DisplayName               = u.DisplayName!,
                            GivenName                 = u.GivenName!,
                            Surname                   = u.Surname!,
                            Mail                      = u.Mail!,
                            UserPrincipalName         = u.UserPrincipalName!,
                            MailNickname              = u.MailNickname!,
                            JobTitle                  = u.JobTitle!,
                            Department                = u.Department!,
                            OfficeLocation            = u.OfficeLocation!,
                            MobilePhone               = u.MobilePhone!,
                            BusinessPhones            = u.BusinessPhones?.ToList() ?? [],
                            OtherMails                = u.OtherMails?.ToList()     ?? [],
                            OnPremisesImmutableId     = u.OnPremisesImmutableId ?? string.Empty,
                            UsageLocation             = u.UsageLocation!,
                            PreferredLanguage         = u.PreferredLanguage!,
                            UserType                  = u.UserType!,
                            CreatedDateTime           = u.CreatedDateTime,
                            LastPasswordChangeDateTime= u.LastPasswordChangeDateTime,
                            UserMailboxSettings       = existingUser?.UserMailboxSettings ?? new(),
                            MailboxFolders            = existingUser?.MailboxFolders      ?? [],
                            FoldersDeltaLink          = existingUser?.FoldersDeltaLink,
                        };

                        if (byId.TryGetValue(u.Id!, out var existing))
                        {
                            dbContext.Entry(existing).CurrentValues.SetValues(mapped);
                        }
                        else
                        {
                            await dbContext.Users.AddAsync(mapped, cancellationToken);
                            byId[u.Id!] = mapped;
                        }
                    }
                }

                nextLink = resp.OdataNextLink;
                newDelta = resp.OdataDeltaLink;
                if (nextLink != null)
                    builder = builder.WithUrl(nextLink);

            } while (nextLink != null);

            state.DeltaLink   = newDelta;
            state.LastSyncUtc = DateTimeOffset.UtcNow;
            // dbContext.SyncStates.Update(state);

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

    

    public async Task SyncUserFolderAsync(ExchangeUser user, CancellationToken cancellationToken)
    {
        var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var baseBuilder = graph.Users[user.UserPrincipalName].MailFolders.Delta;
        var builder = string.IsNullOrEmpty(user.FoldersDeltaLink)
            ? baseBuilder
            : baseBuilder.WithUrl(user.FoldersDeltaLink);
        
        var byId = user.MailboxFolders.ToDictionary(f => f.Id, StringComparer.OrdinalIgnoreCase);
        List<ExchangeMailFolder> folders = new();
        string? nextLink = null;
        string? deltaLink = null;
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
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
                        "unreadItemCount",
                        "lastModifiedDateTime"
                    ];
                    requestConfig.QueryParameters.Top = 100;
                }, cancellationToken);
                if(resp is null) continue;

                if (resp.Value is not null && resp.Value.Count > 0)
                {
                    foreach (var f in resp.Value)
                    {
                        // Either remove record or set a property to delete
                        if (f.AdditionalData.TryGetValue("@removed", out _))
                        {
                            if (byId.TryGetValue(f.Id!, out var toRemove))
                            {
                                dbContext.MailFolders.Remove(toRemove);
                                byId.Remove(f.Id!);
                            }
                        }
                        else
                        {
                            // b) Map to your domain type
                            var mapped = new ExchangeMailFolder
                            {
                                Id                   = f.Id!,
                                DisplayName          = f.DisplayName!,
                                ParentFolderId       = f.ParentFolderId,
                                ChildFolderCount     = f.ChildFolderCount ?? 0,
                                TotalItemCount       = f.TotalItemCount ?? 0,
                                UnreadItemCount      = f.UnreadItemCount ?? 0,
                                LastModifiedDateTime = DateTimeOffset.UtcNow,
                                User                 = user
                            };

                            if (byId.TryGetValue(f.Id!, out var existing))
                            {
                                // update existing
                                existing.DisplayName          = mapped.DisplayName;
                                existing.ParentFolderId       = mapped.ParentFolderId;
                                existing.ChildFolderCount     = mapped.ChildFolderCount;
                                existing.TotalItemCount       = mapped.TotalItemCount;
                                existing.UnreadItemCount      = mapped.UnreadItemCount;
                                existing.LastModifiedDateTime = mapped.LastModifiedDateTime;

                                dbContext.MailFolders.Update(existing);
                            }
                            else
                            {
                                await dbContext.MailFolders.AddAsync(mapped, cancellationToken);
                                byId[f.Id!] = mapped;
                            }
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
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}