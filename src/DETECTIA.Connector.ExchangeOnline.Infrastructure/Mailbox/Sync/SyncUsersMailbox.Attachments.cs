using DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Task = System.Threading.Tasks.Task;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Mailbox.Sync;

public partial class SyncUsersMailbox
{
     public async Task SyncUsersMessageAttachmentsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        
        var msgByUser = await (
                from m in dbContext.UserMessages.AsNoTracking()
                join u in dbContext.Users.AsNoTracking()
                    on m.UserId equals u.Id
                select new { u.UserPrincipalName, m.GraphId, m.Id }
            ).ToListAsync(cancellationToken);
        
        var attachments = new List<UserMessageAttachment>();
        foreach (var message in msgByUser)
        {
            try
            {
                // 2) Fetch the attachments metadata
                var resp = await graph
                    .Users[message.UserPrincipalName]
                    .Messages[message.GraphId]
                    .Attachments
                    .GetAsync(cfg =>
                    {
                        cfg.QueryParameters.Select = new[]
                        {
                            "id",
                            "name",
                            "contentType",
                            "size",
                            "isInline",
                            "lastModifiedDateTime"
                        };
                    }, cancellationToken) ?? new();
                
                if(resp.Value is null) continue;
                    
                
                foreach (var a in resp.Value)
                {
                    attachments.Add(new UserMessageAttachment
                    {
                        Id                    = 0,              
                        MessageId             = message.Id,    
                        GraphId               = a.Id!,
                        Name                  = a.Name!,
                        ContentType           = a.ContentType!,
                        Size                  = a.Size ?? 0,
                        IsInline              = a.IsInline ?? false,
                        LastModifiedAt  = a.LastModifiedDateTime,
                        HasBeenScanned        = false,
                    });
                }

            }
            catch (ODataError ex)
            {
                if (ex.Message.Contains("inactive",   StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("soft-deleted",StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("not found",   StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Skipping attachments for message {MsgId}: {Error}",
                        message.Id, ex.Message);
                    continue;
                }
                throw;
            }
        }

        await dbContext.BulkInsertOrUpdateAsync(attachments, new BulkConfig
        {
            UpdateByProperties = [
            
                nameof(UserMessageAttachment.GraphId)
            ],
            PropertiesToExcludeOnUpdate = [
                nameof(UserMessageAttachment.Id),
                nameof(UserMessageAttachment.MessageId)
            ],
            SetOutputIdentity   = false,
            PreserveInsertOrder = false,
            BatchSize           = 500
        }, cancellationToken: cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}