using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record MessageMatch : Match
{
    public long?                 MessageId            { get; set; }
    public UserMessage?          Message              { get; set; }
                                                      
    public long?                 AttachmentId         { get; set; }
    public MessageAttachment?    Attachment           { get; set; }
}

public record EventMatch : Match
{
    public long?                 EventId            { get; set; }
    public CalendarEvent?        Event              { get; set; }
                                                      
    public long?                 AttachmentId         { get; set; }
    public EventAttachment?      Attachment           { get; set; }
}

public record TaskMatch : Match
{
    
}

public abstract record Match
{
    [Key]
    public long Id                                    { get; init; }
    public required string Name                       { get; set; }
    public required string Pattern                    { get; set; }
    public required int MatchCount                    { get; set; }
}


