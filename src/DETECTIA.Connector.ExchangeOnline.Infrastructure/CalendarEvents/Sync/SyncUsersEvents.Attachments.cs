using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using DETECTIA.Connector.ExchangeOnline.Migration;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.CalendarEvents.Sync;

public partial class SyncUsersEvents
{
    public async Task SyncAttachmentsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);

        var calendarEvents = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.HasBeenScanned == false)
            .Where(e => !string.IsNullOrEmpty(e.GraphId))
            .Include(e=>e.Organizer)
            .ToListAsync(cancellationToken);

        var eventAttachments = new List<EventAttachment>();

        foreach (var ev in calendarEvents)
        {
            try
            {
                var attachments = await graph.Users[ev.Organizer!.UserPrincipalName]
                    .Events[ev.GraphId]
                    .Attachments
                    .GetAsync(cancellationToken: cancellationToken);

                if (attachments?.Value == null)
                    continue;

                foreach (var attachment in attachments.Value)
                {
                    if (attachment is Microsoft.Graph.Models.FileAttachment file)
                    {
                        eventAttachments.Add(new EventAttachment
                        {
                            GraphId = attachment.Id,
                            Size = attachment.Size,
                            EventId = ev.Id,
                            Name = file.Name,
                            ContentType = file.ContentType,
                            IsInline = file.IsInline ?? false,
                            HasBeenScanned = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch attachments for Event {GraphId}", ev.GraphId);
            }

            if (eventAttachments.Count < 500) continue;
            await FlushAsync(dbContext, eventAttachments, cancellationToken);
            eventAttachments.Clear();
        }

        await FlushAsync(dbContext, eventAttachments, cancellationToken);
    }
    
    
    private static async Task FlushAsync(
        AppDatabaseContext dbContext, 
        List<EventAttachment> attachments, 
        CancellationToken cancellationToken)
    {
        if (attachments.Count > 0)
        {
            await dbContext.BulkInsertOrUpdateAsync(attachments, new BulkConfig
            {
                UpdateByProperties             = [nameof(CalendarEvent.GraphId) ],
                PropertiesToExcludeOnUpdate    = [nameof(CalendarEvent.GraphId), nameof(CalendarEvent.Id)] ,
                SetOutputIdentity              = false,
                PreserveInsertOrder            = false,
                BatchSize                      = 500
            }, cancellationToken: cancellationToken);
        }
    }

}