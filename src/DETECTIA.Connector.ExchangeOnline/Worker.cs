using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services.ContentSearchScan;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services.Sync;

namespace DETECTIA.Connector.ExchangeOnline;

public class Worker(
    ILogger<Worker> logger,
    SyncMetadata sync,
    MessageScan messageScan,
    MessageAttachmentScan attachmentScan,
    UserEventsSync sync2) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            //await sync.SyncUsersAsync(stoppingToken);
            //await sync.SyncUsersMailboxSettingsAsync(stoppingToken);
            //await sync.SyncUsersFoldersAsync(stoppingToken);
            //await sync.SyncAllUsersMessagesAsync(stoppingToken);
            //await sync.SyncAllUsersMessageAttachmentsAsync(stoppingToken);
            await sync2.SyncUsersEventsAsync(stoppingToken);

            //await messageScan.ScanEmailTextAsync(stoppingToken);
            //await attachmentScan.ScanEmailAttachmentsAsync(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }
}