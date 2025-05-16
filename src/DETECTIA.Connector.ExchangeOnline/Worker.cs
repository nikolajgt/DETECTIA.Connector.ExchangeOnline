using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using Microsoft.Graph;

namespace DETECTIA.Connector.ExchangeOnline;

public class Worker(
    ILogger<Worker> logger,
    SyncMetadata sync) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            
            
            await sync.SyncUsersAsync(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }
}