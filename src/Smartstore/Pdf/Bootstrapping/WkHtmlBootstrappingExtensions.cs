using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Pdf;
using Smartstore.Pdf.WkHtml;

namespace Smartstore.Bootstrapping
{
    public static class WkHtmlBootstrappingExtensions
    {
        public static IServiceCollection AddWkHtmlToPdf(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            // Don't overwrite any options setups that a user may have added.
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<WkHtmlToPdfOptions>, WkHtmlToPdfOptionsSetup>());

            services.AddTransient<IPdfConverter, WkHtmlToPdfConverter>();
            services.AddSingleton<IWkHtmlCommandBuilder, WkHtmlCommandBuilder>();

            return services;
        }

        public static IServiceCollection AddWkHtmlToPdf(this IServiceCollection services, Action<WkHtmlToPdfOptions> setupAction)
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(setupAction, nameof(setupAction));

            services.AddWkHtmlToPdf();
            services.Configure(setupAction);

            return services;
        }

        class WkHtmlToPdfOptionsSetup : IConfigureOptions<WkHtmlToPdfOptions>
        {
            private readonly IApplicationContext _appContext;

            public WkHtmlToPdfOptionsSetup(IApplicationContext appContext)
            {
                _appContext = appContext;
            }

            public void Configure(WkHtmlToPdfOptions options)
            {
                Guard.NotNull(options, nameof(options));

                options.TempFilesPath = _appContext.GetTenantTempDirectory("PdfGen").PhysicalPath.EnsureEndsWith(Path.DirectorySeparatorChar);

                var baseUrl = _appContext.AppConfiguration.PdfEngineBaseUrl.TrimSafe().NullEmpty();
                if (baseUrl != null)
                {
                    try
                    {
                        options.BaseUrl = new Uri(baseUrl, UriKind.Absolute);
                    }
                    catch (Exception ex)
                    {
                        throw new FormatException($"Malformed Uri string in '{nameof(SmartConfiguration.PdfEngineBaseUrl)}'.", ex);
                    }
                }
            }
        }
    }
}
