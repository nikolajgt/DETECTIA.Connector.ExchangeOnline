using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public class MessageAttachment
{
    [Key]
    public long Id                                     { get; init; }
    
    /// <summary>
    /// FK to the parent message in your DB.
    /// </summary>
    public required long MessageId { get; set; }
    
    public required string GraphId { get; set; }

    /// <summary>
    /// The file name (e.g. "report.pdf").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// MIME content type (e.g. "application/pdf").
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Size in bytes.
    /// </summary>
    public required int Size { get; set; }

    /// <summary>
    /// True if this was an inline (embedded) attachment.
    /// </summary>
    public bool IsInline { get; set; }
    public bool IsDeleted { get; set; }

    public required bool HasBeenScanned { get; set; }
    public bool? IsSensitive { get; set; }

    /// <summary>
    /// When the attachment was last modified on the server.
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
    public DateTimeOffset? ScannedAt { get; set; }
    [ForeignKey(nameof(MessageId))]
    public UserMessage? Message      { get; init; }  
}