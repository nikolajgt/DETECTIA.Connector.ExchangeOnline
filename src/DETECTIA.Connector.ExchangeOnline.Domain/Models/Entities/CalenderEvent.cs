namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record CalenderEvent
{
    public string Id { get; set; }
    public string Subject { get; set; }
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
    public string LocationDisplayName { get; set; }
    public bool? IsAllDay { get; set; }
    public bool? IsCancelled { get; set; }
    public string ShowAs { get; set; } // free, tentative, busy, oof, etc.
    public string BodyPreview { get; set; }
    public string BodyContent { get; set; }
    public string BodyContentType { get; set; } // text, html
    public string OrganizerName { get; set; }
    public string OrganizerEmail { get; set; }
    public string Importance { get; set; }
    public List<string> Categories { get; set; }
    
    public List<User> Attendees { get; set; }
}