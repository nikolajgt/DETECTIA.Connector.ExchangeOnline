

using DETECTIA.Connector.ExchangeOnline.Migration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Sync;

public partial class SyncUsersEvents(
    ILogger<SyncUsersEvents> logger, 
    GraphServiceClient graph, 
    IDbContextFactory<AppDatabaseContext> dbFactory)
{
    
}