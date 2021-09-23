using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
                ServiceDescriptor.Singleton<IConfigureOptions<WkHtmlToPdfOptions>, WkHtmlToPdfOptionsSetup>());

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
            public void Configure(WkHtmlToPdfOptions options)
            {
                Guard.NotNull(options, nameof(options));

                string rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                string path;
                string runtimeArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    path = Path.Combine(rootPath, "runtimes\\win-", runtimeArchitecture, "\\native", "wkhtmltopdf.exe");
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    path = Path.Combine(rootPath, "runtimes\\linux-", runtimeArchitecture, "\\native", "wkhtmltopdf");
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    path = Path.Combine(rootPath, "runtimes\\osx-", runtimeArchitecture, "\\native", "wkhtmltopdf");
                else
                    throw new InvalidOperationException("Supported OS Platform not found");

                // TODO: (core) Complete
                options.PdfToolPath = "";
                options.PdfToolName = "";
                options.TempFilesPath = "";
            }
        }
    }
}
