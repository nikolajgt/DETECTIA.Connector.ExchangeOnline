using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record MailAttachment : Attachment
{
    public required long MailId { get; init; }
    public List<MailMatch>? Matches { get; init; }
}

public record EventAttachment : Attachment
{
    public required long EventId { get; init; }
    public List<EventMatch>? Matches { get; init; }
}

public record TaskAttachment : Attachment
{
    public required long TaskId { get; init; }
    public List<TaskMatch>? Matches { get; init; }
}

/// <summary>
/// Represents a base attachment object, such as those found on emails, calendar events, or tasks.
/// </summary>
public abstract record Attachment
{
    /// <summary>The internal database identity key for this attachment.</summary>
    [Key]
    public long              Id                { get; init; }

    /// <summary>The unique identifier assigned by Microsoft Graph to this attachment.</summary>
    public required string   GraphId           { get; init; }

    /// <summary>The display name of the file (e.g., "report.pdf").</summary>
    public required string   Name              { get; init; }

    /// <summary>The MIME content type of the attachment (e.g., "application/pdf").</summary>
    public required string   ContentType       { get; init; }

    /// <summary>The size of the attachment in bytes.</summary>
    public int?              Size              { get; init; }

    /// <summary>Indicates whether the attachment is inline (embedded within the body).</summary>
    public bool              IsInline          { get; init; }

    /// <summary>Indicates whether the attachment has been deleted from the source.</summary>
    public bool              IsDeleted         { get; init; }

    /// <summary>The timestamp when the attachment was last modified on the server.</summary>
    public DateTimeOffset?   LastModifiedAt    { get; set; }

    /// <summary>Indicates whether this attachment has been scanned for sensitive content.</summary>
    public required bool     HasBeenScanned    { get; set; }

    /// <summary>Indicates whether this attachment contains sensitive data. Null if not scanned.</summary>
    public bool?             IsSensitive       { get; set; }

    /// <summary>The timestamp when the attachment was last scanned.</summary>
    public DateTimeOffset?   ScannedAt         { get; set; }
}
