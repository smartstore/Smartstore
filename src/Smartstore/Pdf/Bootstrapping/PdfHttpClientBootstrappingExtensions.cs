#nullable enable

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Net.Http;
using Smartstore.Pdf;

namespace Smartstore.Bootstrapping;

/// <summary>Provides registration for the PDF HTTP client.</summary>
public static class PdfHttpClientBootstrappingExtensions
{
    /// <summary>Registers a PDF HTTP client with isolated request cookies and a ten-second timeout.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPdfHttpClient(this IServiceCollection services)
    {
        Guard.NotNull(services);

        services.AddHttpClient<PdfHttpClient>()
            .AddSmartstoreUserAgent()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // Factory handlers are pooled. Cookies must come exclusively from each snapshot.
                UseCookies = false,
                // Redirects could send an explicitly supplied Cookie header to another origin.
                AllowAutoRedirect = false
            });

        return services;
    }
}
