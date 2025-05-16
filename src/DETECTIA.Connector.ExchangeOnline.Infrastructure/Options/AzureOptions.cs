namespace DETECTIA.Connector.ExchangeOnline.Infrastructure.Options;

public record AzureOptions
{
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string Thumbprint { get; init; }
    public required int MaxRetryCount { get; init; }
    public required int RetryDelayInMilliseconds { get; init; }
}

