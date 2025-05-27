using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;


public partial class SyncUsersMailbox
{
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
                    UserId = u.Id,
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
        }, cancellationToken: cancellationToken);
    }
}