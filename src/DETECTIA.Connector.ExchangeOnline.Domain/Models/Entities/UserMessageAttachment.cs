using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

[Index(nameof(GraphId))]
public class UserMessageAttachment
{
    [Key]
    public long Id                                     { get; init; }
    
    /// <summary>
    /// FK to the parent message in your DB.
    /// </summary>
    public required long MessageId { get; init; }
    
    public required string GraphId { get; init; }

    /// <summary>
    /// The file name (e.g. "report.pdf").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// MIME content type (e.g. "application/pdf").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Size in bytes.
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    /// True if this was an inline (embedded) attachment.
    /// </summary>
    public bool IsInline { get; init; }
    public bool IsDeleted { get; init; }

    public required bool HasBeenScanned { get; set; }
    public bool? IsSensitive { get; set; }

    /// <summary>
    /// When the attachment was last modified on the server.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
    public DateTimeOffset? ScannedAt { get; set; }

    public List<Match> Matches { get; set; } = [];
    [ForeignKey(nameof(MessageId))]
    public UserMessage? Message      { get; init; }  
}