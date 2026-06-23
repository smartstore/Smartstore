using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Net.Http;

namespace Smartstore.Bootstrapping;

public static class HttpBootstrappingExtensions
{
    /// <summary>
    /// Adds download manager Http client.
    /// </summary>
    public static IServiceCollection AddDownloadManager(this IServiceCollection services)
    {
        Guard.NotNull(services, nameof(services));

        services
            .AddHttpClient<DownloadManager>((sp, client) =>
            {
                var cache = new CacheControlHeaderValue { NoCache = true };

                // TODO: (core) What about downloadTimeout and maxCachingAge parameters?

                client.Timeout = TimeSpan.FromMinutes(5);

                client.DefaultRequestHeaders.CacheControl = cache;
                client.DefaultRequestHeaders.Add("Connection", "Keep-alive");
            })
            // Disable automatic redirect following so DownloadManager can re-validate each redirect target (SSRF).
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        return services;
    }
}