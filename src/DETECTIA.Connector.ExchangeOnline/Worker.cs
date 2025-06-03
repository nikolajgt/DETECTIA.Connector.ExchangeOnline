using DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Scan;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Sync;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Scan;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Users;

namespace DETECTIA.Connector.ExchangeOnline;

public class Worker(
    ILogger<Worker> logger,
    SyncUsers syncUsers,
    SyncUsersEvents syncEvents,
    SyncUsersMailbox syncMailbox,
    ScanUsersMailbox scanMailbox,
    ScanUsersEvents scanEvents) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // users
            // await syncUsers.SyncUsersAsync(stoppingToken);
            // await syncUsers.SyncGroupsAsync(stoppingToken);
            // users mailbox
            // await syncMailbox.SyncUsersMailboxSettingsAsync(stoppingToken);
            // await syncMailbox.SyncUsersMailboxFoldersAsync(stoppingToken);
            // await syncMailbox.SyncUsersMessagesAsync(stoppingToken);
            // await syncMailbox.SyncUsersMessageAttachmentsAsync(stoppingToken);
            // await scanMailbox.ScanUsersMessageTextAsync(stoppingToken);
            // await scanMailbox.ScanUsersMessageAttachmentsAsync(stoppingToken);
            
            // users calendar events
            await syncEvents.SyncUsersEventsAsync(stoppingToken);
            await syncEvents.SyncCalendarEventAttachmentsAsync(stoppingToken);
            await scanEvents.ScanEventsTextAsync(stoppingToken);
            await scanEvents.ScanEventAttachmentsAsync(stoppingToken);
            
            logger.LogInformation("Finished");
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }
}