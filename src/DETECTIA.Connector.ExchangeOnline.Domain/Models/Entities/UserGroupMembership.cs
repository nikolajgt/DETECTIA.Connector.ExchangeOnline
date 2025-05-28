using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record UserGroupMembership
{
    [Key]
    public long Id { get; init; }
    public required long UserId { get; set; }
    public required User User { get; set; }

    public required long GroupId { get; set; }
    public required UserGroup Group { get; set; }
}