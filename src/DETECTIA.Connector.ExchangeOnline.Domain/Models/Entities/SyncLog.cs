namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record SyncLog
{
    /// <summary>
    /// Primary key for the sync log entry.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// The UTC timestamp when the sync attempt started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// The UTC timestamp when the sync attempt ended.
    /// </summary>
    public DateTimeOffset? EndedAtUtc { get; set; }

    /// <summary>
    /// The sync target type: Mailbox, Folder, etc.
    /// </summary>
    public required string TargetType { get; set; } // e.g. "Mailbox", "Folder"

    /// <summary>
    /// The identifier of the target (MailboxId or MailFolderId).
    /// </summary>
    public required string TargetIdentifier { get; set; }

    /// <summary>
    /// Whether the sync succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of items processed during the sync (added, updated, or deleted).
    /// </summary>
    public int? ItemsChangedCount { get; set; }

    /// <summary>
    /// Message or summary of what occurred during the sync.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The exception message or stack trace, if the sync failed.
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// The delta link used for this sync attempt (optional).
    /// </summary>
    public string? DeltaLinkUsed { get; set; }

    /// <summary>
    /// Arbitrary duration of the sync operation, for metrics/logging.
    /// </summary>
    public TimeSpan? Duration => EndedAtUtc.HasValue ? EndedAtUtc - StartedAtUtc : null;
}
