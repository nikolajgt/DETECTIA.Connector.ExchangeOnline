using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Pipelines;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;
using User = DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities.User;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Sync;

public partial class SyncUsersEvents
{
    // public async Task SyncEventParticipantsAsync(CancellationToken cancellationToken)
    // {
    //      await DataflowSyncPipeline.RunAsync<CalendarEvent, EventParticipant>(
    //         fetchPageAsync: async (lastUserId, ct) =>
    //         {
    //             await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
    //             return await dbContext.Events
    //                 .AsNoTracking()
    //                 .Where(u => u.Id > lastUserId)
    //                 .OrderBy(u => u.Id)
    //                 .Take(100)
    //                 .ToListAsync(ct);
    //         },
    //         expandAsync: async (user, ct) =>
    //         {
    //             try
    //             {
    //              
    //                 
    //                 return [mapped];
    //             }
    //             catch (ODataError ex)
    //             {
    //                 if (!ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase) &&
    //                     !ex.Message.Contains("soft-deleted", StringComparison.OrdinalIgnoreCase) &&
    //                     !ex.Message.Contains("on-premise", StringComparison.OrdinalIgnoreCase))
    //                 {
    //                     throw;
    //                 }
    //
    //                 logger.LogWarning(
    //                     "Skipping mailboxSettings for {Upn}: {Error}",
    //                     user.UserPrincipalName,
    //                     ex.Message);
    //                 
    //                 return [];
    //             }
    //         },
    //         persistBatchAsync: async (eventParticipants, ct) =>
    //         {
    //             await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
    //         
    //         },
    //         keySelector: u => u.Id,
    //         persistBatchSize: 100, // Adjust as needed
    //         maxDegreeOfParallelism: 4,
    //         cancellationToken: cancellationToken
    //     );
    // }
}