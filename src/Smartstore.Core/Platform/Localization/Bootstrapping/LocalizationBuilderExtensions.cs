using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;

namespace Smartstore.Core.Bootstrapping
{
    public static class LocalizationBuilderExtensions
    {
        public static IMvcBuilder AddAppLocalization(this IMvcBuilder builder)
        {
            builder.Services.AddRouting(o =>
            {
                o.ConstraintMap["culture"] = typeof(CultureRouteConstraint);
            });

            //builder.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            builder.AddDataAnnotationsLocalization(options =>
            {
                // 
            });

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, AppLocalizationMvcOptionsSetup>());

            return builder;
        }

        /// <summary>
        /// Uses culture from current working language and sets globalization clients scripts accordingly.
        /// </summary>
        public static IApplicationBuilder UseRequestCulture(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestCultureMiddleware>();
        }
    }

    internal class AppLocalizationMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public AppLocalizationMvcOptionsSetup(IServiceProvider serviceProvider)
            : base(ConfigureMvc)
        {
            _serviceProvider = serviceProvider;
        }

        public override void Configure(MvcOptions options)
        {
            base.Configure(options);
            options.Conventions.Add(new LocalizedRoutingConvention(_serviceProvider));
        }

        public static void ConfigureMvc(MvcOptions options)
        {
        }
    }
}
