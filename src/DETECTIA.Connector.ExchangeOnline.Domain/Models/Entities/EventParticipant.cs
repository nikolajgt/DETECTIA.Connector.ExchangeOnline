using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;


public record EventParticipant
{                      
    [Key]
    public long            Id               { get; init; }
    
    public long            EventId          { get; set; }
    public Event?          Event            { get; set; }

    public long            UserId           { get; set; }
    public User?           User             { get; set; }

    public string          Type             { get; set; }          
    public string          StatusResponse   { get; set; } 
}