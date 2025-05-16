using DETECTIA.Connector.ExchangeOnline;
using DETECTIA.Connector.ExchangeOnline.Infrastructure;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Serilog;
using Constants = DETECTIA.Connector.ExchangeOnline.Constants;

await new HostBuilder()
    .UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    .ConfigureLogging((context, builder) =>
    {
        builder.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(context.Configuration)
            .CreateLogger();

        builder.AddSerilog(dispose: true);
    })
    .ConfigureAppConfiguration(ConfigureAppConfiguration)
    .ConfigureServices(ConfigureServices)
    .Build()
    .RunAsync();

return;

static void ConfigureAppConfiguration(
    HostBuilderContext context,
    IConfigurationBuilder config)
{
    config
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddUserSecrets<AssemblyPointer>()
        .AddEnvironmentVariables(prefix: Constants.EnvPrefix);
}

static void ConfigureServices(
    HostBuilderContext context,
    IServiceCollection services)
{
    
    services.AddInfrastructure(context.Configuration);
    
    services.AddHostedService<Worker>();
}
