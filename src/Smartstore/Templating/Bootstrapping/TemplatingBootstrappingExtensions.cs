using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Templating;

namespace Smartstore.Bootstrapping
{
    public static class TemplatingBootstrappingExtensions
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