using DETECTIA.Connector.ExchangeOnline.Migration;
using DETECTIA.ContentSearch.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Scan;

public partial class ScanUsersMailbox(
    IContentSearch contentSearch,
    ILogger<ScanUsersMailbox> logger,
    GraphServiceClient graph,
    IDbContextFactory<AppDatabaseContext> dbFactory)
{
    public async Task ScanAsync(CancellationToken cancellationToken)
    {
        await ScanUsersMessageTextAsync(cancellationToken);
        await ScanUsersMessageAttachmentsAsync(cancellationToken);
    }
}