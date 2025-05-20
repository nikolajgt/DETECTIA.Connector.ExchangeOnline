using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record UserMessage
{
    [Key]
    public long Id                                     { get; init; }
    public required string  MessageId                  { get; init; }  
    public required long   FolderId                    { get; init; }  

    
                                                       
    public string?           Subject                   { get; init; }
    public string?           From                      { get; init; }
    public IList<string> ToRecipients                  { get; init; } = [];
    public required DateTimeOffset ReceivedDateTime    { get; init; }
    public required bool     IsRead                    { get; init; }
    public string?           InternetMessageId         { get; init; }
    
    public List<UserMessageAttachements>? Attachments  { get; init; } = [];
}