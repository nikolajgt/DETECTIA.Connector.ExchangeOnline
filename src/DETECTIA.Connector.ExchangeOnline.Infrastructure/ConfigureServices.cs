using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Azure.Identity;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Options;
using DETECTIA.Connector.ExchangeOnline.Infrastructure.Services;
using DETECTIA.Connector.ExchangeOnline.Migration;
using DETECTIA.ContentSearch;
using DETECTIA.ContentSearch.Application;
using DETECTIA.ContentSearch.Infrastructure.SearchServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Polly;

namespace DETECTIA.Connector.ExchangeOnline.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureOptions>(configuration.GetRequiredSection("AzureAd"));
        services.AddEntityFramework(configuration);
        services.AddSingleton(provider =>
        {
            var azureOptions                        = provider.GetRequiredService<IOptions<AzureOptions>>();

            const StoreName clientStoreName         = StoreName.My;
            const StoreLocation clientStoreLocation = StoreLocation.CurrentUser;
            using var store                         = new X509Store(clientStoreName, clientStoreLocation);
            store.Open(OpenFlags.ReadOnly);
            var certificate                         = store.Certificates
                .Find(X509FindType.FindByThumbprint, azureOptions.Value.Thumbprint, validOnly: false)
                // ReSharper disable once RedundantEnumerableCastCall
                .OfType<X509Certificate2>()
                .FirstOrDefault();
            
            var credential = new ClientCertificateCredential(
                azureOptions.Value.TenantId,
                azureOptions.Value.ClientId,
                certificate
            );

            var scopes                         = new[] { "https://graph.microsoft.com/.default" };
            var authProvider                   = new AzureIdentityAuthenticationProvider(credential, scopes: scopes);
            var loggerFactory                  = provider.GetRequiredService<ILoggerFactory>();
            var rateLimiterLogger              = loggerFactory.CreateLogger("rateLimiterLogger");
            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                    .OrResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
                    .OrResult(r => r.StatusCode == HttpStatusCode.BadGateway)
                    .OrResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                    .OrResult(r => r.StatusCode == HttpStatusCode.GatewayTimeout)
                    .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: azureOptions.Value.MaxRetryCount, 
                    sleepDurationProvider: (retryAttempt, _) => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, _) =>
                    {
                        rateLimiterLogger.LogWarning(
                            "Graph returned 429. Waiting {Delay}ms before retry {RetryAttempt}.",
                            timespan.TotalMilliseconds,
                            retryAttempt
                        );
                    }
                );

            var throttleHandler                 = new PolicyHttpMessageHandler(retryPolicy);
            throttleHandler.InnerHandler        = new HttpClientHandler();
            var httpClient = GraphClientFactory.Create([
                throttleHandler
            ]);

            var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
            return new GraphServiceClient(adapter);
        });
        var regexPatterns = configuration
            .GetSection("ContentSearch:RegexPattern")
            .Get<List<string>>()!
            .Select(x => new Regex(x))
            .ToList();
        
        
        services.AddTransient<ISearchTextService, SearchTextService>();
        services.AddContentSearch(regexPatterns);
        services.AddTransient<SyncMetadata>();
        services.AddTransient<ContentScan>();
        return services;
    }
}