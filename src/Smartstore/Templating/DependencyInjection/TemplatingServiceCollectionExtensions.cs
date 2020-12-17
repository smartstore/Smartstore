using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore;
using Smartstore.Templating;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TemplatingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a singleton template manager and a void <see cref="ITemplateEngine"/>.
        /// </summary>
        public static IServiceCollection AddTemplateEngine(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.TryAddSingleton<ITemplateEngine, NullTemplateEngine>();
            services.TryAddSingleton<ITemplateManager, DefaultTemplateManager>();

            return services;
        }
    }
}