using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public record Match
{
    [Key]
    public long Id { get; init; }
    public string Name { get; set; }
    public string Pattern { get; set; }
    public int MatchCount { get; set; }
}