using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

[Index(nameof(GraphId))]
public record CalendarEvent
{
    [Key]
    public long Id { get; init; }
    public required string GraphId { get; set; }
    public string? Subject { get; set; }
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
    public string? LocationDisplayName { get; set; }
    public bool? IsAllDay { get; set; }
    public bool? IsCancelled { get; set; }
    public string? ShowAs { get; set; } 
    public string? BodyContentType { get; set; } 
    public long OrganizerId          { get; set; }
    public User? Organizer { get; set; }
    public string? Importance { get; set; }
    public List<string> Categories { get; set; } = [];
    
    public required bool HasBeenScanned      { get; set; }
    public bool? IsSensitive { get; set; }
    public DateTimeOffset? ScannedAt                   { get; set; }

    public List<EventAttachment>? Attachments { get; set; } = [];
    public List<EventParticipant>? Attendees { get; set; } = [];
}