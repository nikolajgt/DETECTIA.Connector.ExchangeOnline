using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;


[Index(nameof(EventId), nameof(UserId))]
public record EventParticipant
{                      
    [Key]
    public long            Id               { get; init; }
    
    public long            EventId          { get; set; }
    public CalendarEvent?          Event            { get; set; }

    public long            UserId           { get; set; }
    public User?           User             { get; set; }

    public string          Type             { get; set; }          
    public string          StatusResponse   { get; set; } 
}