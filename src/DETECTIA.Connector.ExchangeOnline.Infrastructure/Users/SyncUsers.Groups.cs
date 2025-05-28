using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Users;

public partial class SyncUsers
{
    public async Task SyncGroupsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var state = await dbContext.SyncStates.SingleOrDefaultAsync(s => s.Key == "GroupsDelta", cancellationToken);
            var isNew = state == null;
            if (isNew)
            {
                state = new ExchangeSyncState { Key = "GroupsDelta" };
                var entityResponse = await dbContext.SyncStates.AddAsync(state, cancellationToken);
                state = entityResponse.Entity;
            }

            var baseBuilder = graph.Groups.Delta;
            var builder = string.IsNullOrEmpty(state.DeltaLink)
                ? baseBuilder
                : baseBuilder.WithUrl(state.DeltaLink);

            var groups = new List<UserGroup>();
            string? nextLink = null, newDelta = null;
            
            do
            {
                var resp = await builder.GetAsDeltaGetResponseAsync(cfg =>
                {
                    cfg.QueryParameters.Select = new[]
                    {
                        "id",
                        "displayName",
                        "description",
                        "mail",
                        "mailNickname",
                        "mailEnabled",
                        "securityEnabled",
                        "groupTypes",
                        "createdDateTime",
                        "visibility"
                    };
                    cfg.QueryParameters.Top = 100;
                }, cancellationToken);

                foreach (var g in resp.Value)
                {
                    if (g.AdditionalData?.ContainsKey("@removed") == true)
                    {
                        var group = await dbContext.UserGroups.SingleOrDefaultAsync(x => x.GraphId == g.Id, cancellationToken);
                        if (group is null) continue;
                        dbContext.UserGroups.Remove(group);
                    }
                    else
                    {
                        groups.Add(new UserGroup
                        {
                            GraphId = g.Id!,
                            DisplayName = g.DisplayName ?? string.Empty,
                            Description = g.Description ?? string.Empty,
                            Mail = g.Mail ?? string.Empty,
                            MailNickname = g.MailNickname ?? string.Empty,
                            MailEnabled = g.MailEnabled,
                            SecurityEnabled = g.SecurityEnabled,
                            GroupTypes = g.GroupTypes?.Any() == true ? string.Join(",", g.GroupTypes) : string.Empty,
                            CreatedDateTime = g.CreatedDateTime,
                            Visibility = g.Visibility ?? string.Empty,
                            Users = null // filled elsewhere via group membership
                        });
                    }
                }

                nextLink = resp.OdataNextLink;
                newDelta = resp.OdataDeltaLink;

                if (nextLink != null)
                    builder = builder.WithUrl(nextLink);

            } while (nextLink != null);

            state.DeltaLink = newDelta;
            state.LastSyncUtc = DateTimeOffset.UtcNow;

            await dbContext.BulkInsertOrUpdateAsync(groups, new BulkConfig
            {
                UpdateByProperties = [nameof(UserGroup.GraphId)],
                PropertiesToExcludeOnUpdate = [nameof(UserGroup.Id), nameof(UserGroup.CreatedDateTime)],
                SetOutputIdentity = false,
                PreserveInsertOrder = false,
                BatchSize = 500
            }, cancellationToken: cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SyncGroupsAsync");
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    private static async Task FlushAsync(
        AppDatabaseContext dbContext, 
        List<EventAttachment> attachments, 
        CancellationToken cancellationToken)
    {
        if (attachments.Count > 0)
        {
            await dbContext.BulkInsertOrUpdateAsync(attachments, new BulkConfig
            {
                UpdateByProperties             = [nameof(CalendarEvent.GraphId) ],
                PropertiesToExcludeOnUpdate    = [nameof(CalendarEvent.GraphId), nameof(CalendarEvent.Id)] ,
                SetOutputIdentity              = false,
                PreserveInsertOrder            = false,
                BatchSize                      = 500
            }, cancellationToken: cancellationToken);
        }
    }

}