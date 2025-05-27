using DETECTIA.Connector.ExchangeOnline.Migration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;

public partial class SyncUsersMailbox(
    ILogger<SyncUsersMailbox> logger,
    GraphServiceClient graph,
    IDbContextFactory<AppDatabaseContext> dbFactory)
{

}