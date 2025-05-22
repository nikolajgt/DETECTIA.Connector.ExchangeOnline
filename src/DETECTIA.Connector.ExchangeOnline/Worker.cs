using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using DETECTIA.Connector.ExchangeOnline.Migration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

namespace DETECTIA.Connector.ExchangeOnline;

public class Worker(
    ILogger<Worker> logger,
    SyncMetadata sync,
    ContentScan scan) : BackgroundService
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
            
            await scan.ScanEmailTextAsync(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }
}