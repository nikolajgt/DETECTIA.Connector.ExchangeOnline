using System.ComponentModel.DataAnnotations;

namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public class ExchangeUsersSyncState
{
    [Key]
    public string Key         { get; set; }
    public string? DeltaLink   { get; set; }
    public DateTimeOffset LastSyncUtc { get; set; }
}