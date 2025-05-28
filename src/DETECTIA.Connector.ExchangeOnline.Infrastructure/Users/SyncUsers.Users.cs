using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Users;

public partial class SyncUsers
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
                        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.GraphId == u.Id, cancellationToken);
                        if (user is null) continue;
                        user.AccountEnabled = false;
                        dbContext.Users.Update(user);
                    }
                    else
                    {
                        users.Add(new User
                        {
                            GraphId = u.Id!,
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
                            CreatedAt = u.CreatedDateTime,
                            LastPasswordChangeAt = u.LastPasswordChangeDateTime,
                            UserMailboxSettings = null, // these get fetched elsewhere
                            MailboxFolders = null,      // these get fetched elsewhere
                            FoldersDeltaLink = null,    // these get fetched elsewhere
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
                UpdateByProperties = [nameof(User.Id)],
                PropertiesToExcludeOnUpdate = [nameof(User.Id), nameof(User.CreatedAt)],
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

}