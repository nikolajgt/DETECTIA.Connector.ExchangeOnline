namespace DETECTIA.Connector.ExchangeOnline.Domain.Models.Entities;

public interface IEntityTracker
{
    DateTimeOffset? LastSyncUtc  { get; set; }
}