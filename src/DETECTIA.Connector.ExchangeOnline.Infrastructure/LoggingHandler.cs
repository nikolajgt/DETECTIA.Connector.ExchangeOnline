using System.Net;
using Microsoft.Extensions.Logging;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure;

public class LoggingHandler(ILogger<LoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Sending request to {Url}", request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        switch (response.StatusCode)
        {
            case HttpStatusCode.TooManyRequests:
                logger.LogWarning("Received 429 TooManyRequests from Graph API.");
                break;

            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                logger.LogWarning("Received {StatusCode} from Graph API (server error).", response.StatusCode);
                break;
        }

        return response;
    }
}