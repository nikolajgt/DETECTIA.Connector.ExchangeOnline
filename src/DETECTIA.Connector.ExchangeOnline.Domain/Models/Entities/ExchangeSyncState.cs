using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public class ExchangeSyncState
{
    [Key]
    public long Id { get; init; }
    public string Key { get; init; }
    public string? DeltaLink { get; set; }
    public DateTimeOffset LastSyncUtc { get; set; }
}