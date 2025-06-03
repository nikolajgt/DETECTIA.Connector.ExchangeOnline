using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;
using User = DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities.User;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;


public partial class SyncUsersMailbox
{
    public async Task SyncUsersMailboxSettingsAsync(CancellationToken cancellationToken)
    {
         await DataflowSyncPipeline.RunAsync<User, UserMailboxSettings>(
            fetchPageAsync: async (lastUserId, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
                return await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id > lastUserId)
                    .OrderBy(u => u.Id)
                    .Take(100)
                    .ToListAsync(ct);
            },
            expandAsync: async (user, ct) =>
            {
                try
                {
                    var graphSettings = await graph
                        .Users[user.UserPrincipalName]
                        .MailboxSettings
                        .GetAsync(cfg =>
                        {
                            cfg.QueryParameters.Select =
                            [
                                "archiveFolder",
                                "automaticRepliesSetting",
                                "dateFormat",
                                "timeFormat",
                                "timeZone",
                                "workingHours",
                                "delegateMeetingMessageDeliveryOptions"
                            ];
                        }, ct);

                    var mapped = new UserMailboxSettings
                    {
                        UserId = user.Id,
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
                    
                    return [mapped];
                }
                catch (ODataError ex)
                {
                    if (!ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase) &&
                        !ex.Message.Contains("soft-deleted", StringComparison.OrdinalIgnoreCase) &&
                        !ex.Message.Contains("on-premise", StringComparison.OrdinalIgnoreCase))
                    {
                        throw;
                    }

                    logger.LogWarning(
                        "Skipping mailboxSettings for {Upn}: {Error}",
                        user.UserPrincipalName,
                        ex.Message);
                    
                    return [];
                }
            },
            persistBatchAsync: async (mailboxSettings, ct) =>
            {
                await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
                await dbContext.BulkInsertOrUpdateAsync(mailboxSettings, new BulkConfig
                {
                    UpdateByProperties = 
                    [
                        nameof(UserMailboxSettings.UserId),
                    ],
                    PropertiesToExcludeOnUpdate = 
                    [
                        nameof(UserMailboxSettings.Id),
                        nameof(UserMailboxSettings.UserId)
                    ],
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    BatchSize = 500
                }, cancellationToken: ct);
            },
            keySelector: u => u.Id,
            persistBatchSize: 100, // Adjust as needed
            maxDegreeOfParallelism: 4,
            cancellationToken: cancellationToken
        );
    }
}