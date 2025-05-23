using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

[Index(nameof(GraphId))]
public record UserMailFolder : IEntityTracker
{
    /// <summary>Graph‐assigned folder ID (GUID).</summary>
    [Key]
    public long Id                     { get; init; }
    
    public required string GraphId { get; set; }
    public required long UserId                        { get; init; }
    

    /// <summary>Human‐readable name (Inbox, Sent Items, etc.).</summary>
    public required string DisplayName            { get; set; }

    /// <summary>Parent folder’s Graph ID, or null for top‐level.</summary>
    public string? ParentFolderId                 { get; set; }

    /// <summary>Number of direct subfolders.</summary>
    public required int ChildFolderCount          { get; set; }

    /// <summary>Total items in this folder (all descendants not counted).</summary>
    public required int TotalItemCount            { get; set; }

    /// <summary>Unread items in this folder.</summary>
    public required int UnreadItemCount           { get; set; }
    
    public required User User              { get; init; }

    /// <summary>
    /// When this folder was last modified (e.g. renamed, moved).
    /// </summary>
    public DateTimeOffset? LastModifiedAt   { get; set; }
    public DateTimeOffset? LastSyncUtc            { get; set; }
    public string? MessagesDeltaLink                          { get; set; }

    
    public List<UserMessage> Messages             { get; set; } = [];
}