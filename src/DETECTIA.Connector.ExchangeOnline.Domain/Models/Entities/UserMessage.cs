using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

[Index(nameof(GraphId))]
public record UserMessage
{
    [Key]
    public long Id                                     { get; init; }
    public required string GraphId                     { get; init; }
    public required long   FolderId                    { get; init; }  
    public required long UserId                        { get; init; }
    public string?           Subject                   { get; init; }
    public string?           From                      { get; init; }
    public IList<string>     ToRecipients              { get; init; } = [];
    public required DateTimeOffset ReceivedAt          { get; init; }
    public required bool     IsRead                    { get; init; }
    public string?           InternetMessageId         { get; init; }
    public required bool HasBeenScanned                { get; set; }
    public bool? IsSensitive                           { get; set; }
    public DateTimeOffset? ScannedAt                   { get; set; }

    public List<MessageMatch>? Matches                 { get; init; } = [];
    public List<MessageAttachment>? Attachments        { get; init; } = [];
    
    
    [ForeignKey(nameof(UserId))]
    public User? User                                  { get; init; }  
}