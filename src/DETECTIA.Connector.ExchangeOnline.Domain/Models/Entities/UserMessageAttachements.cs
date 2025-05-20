using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public class UserMessageAttachements
{
    [Key]
    public long Id                                     { get; init; }
    
    /// <summary>
    /// FK to the parent message in your DB.
    /// </summary>
    public required long MessageId { get; set; }

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

    /// <summary>
    /// The Content-ID header, if inline (for HTML embedding).
    /// </summary>
    public string? ContentId { get; set; }

    /// <summary>
    /// Raw bytes of the file. Only populated if you choose to download it.
    /// </summary>
    public byte[]? ContentBytes { get; set; }

    /// <summary>
    /// When the attachment was last modified on the server.
    /// </summary>
    public DateTimeOffset? LastModifiedDateTime { get; set; }

    /// <summary>
    /// Navigation back to the parent message.
    /// </summary>
    [ForeignKey(nameof(MessageId))]
    public virtual UserMessage? Message { get; set; }
}