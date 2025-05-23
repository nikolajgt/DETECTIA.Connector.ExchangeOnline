namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;


public record EventParticipent
{                                 
    public required long            EventId          { get; set; }
    public required CalenderEvent   GraphEvent       { get; set; }

    public required int             UserId           { get; set; }
    public required User            User             { get; set; }

    public required string          Type             { get; set; }          
    public required string          StatusResponse   { get; set; } 
}