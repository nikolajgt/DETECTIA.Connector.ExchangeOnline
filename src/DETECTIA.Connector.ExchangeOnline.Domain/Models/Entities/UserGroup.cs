using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;


[Index(nameof(GraphId))]
public record UserGroup
{
    [Key]
    public long Id { get; init; }
    public required string GraphId { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Mail { get; set; }
    public string MailNickname { get; set; }
    public bool? MailEnabled { get; set; }
    public bool? SecurityEnabled { get; set; }
    public string GroupTypes { get; set; } // e.g. Unified for Microsoft 365 groups
    public DateTimeOffset? CreatedDateTime { get; set; }
    public string Visibility { get; set; } // Public, Private, HiddenMembership
    
    public List<User>? Users { get; set; }
}