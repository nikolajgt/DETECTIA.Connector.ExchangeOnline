using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record MailMatch : Match;

public record EventMatch : Match;

public record TaskMatch : Match;

public abstract record Match
{
    [Key]
    public long Id                                    { get; init; }
    public required string Name                       { get; set; }
    public required string Pattern                    { get; set; }
    public required int MatchCount                    { get; set; }
    
    public long?   MessageId                           { get; set; }
    public UserMessage?           Message             { get; set; }
                                                      
    public long?   AttachmentId                        { get; set; }
    public UserMessageAttachment? Attachment          { get; set; }
}


