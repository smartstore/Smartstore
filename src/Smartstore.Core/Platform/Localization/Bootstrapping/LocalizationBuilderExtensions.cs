using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Seo.Routing;
using Microsoft.AspNetCore.Http.Features;

namespace Smartstore.Core.Bootstrapping
{
    public static class LocalizationBuilderExtensions
    {
        public static IMvcBuilder AddAppLocalization(this IMvcBuilder builder)
        {
            //builder.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            builder.AddDataAnnotationsLocalization(options =>
            {
                // 
            });

            return builder;
        }

        /// <summary>
        /// Uses culture from current working language and sets globalization clients scripts accordingly.
        /// </summary>
        public static IApplicationBuilder UseRequestCulture(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestCultureMiddleware>();
        }

        /// <summary>
        /// Adds a routing middleware to the pipeline that is aware of {culture} url segments, e.g. "/en/controller/action".
        /// </summary>
        /// <remarks>
        /// A call to <see cref="UseLocalizedRouting(IApplicationBuilder, IApplicationContext)"/> must be followed by a call to
        /// <c>UseEndpoints</c> for the same <see cref="IApplicationBuilder"/>
        /// instance.
        /// <para>
        /// The localized routing middleware defines a point in the middleware pipeline where routing decisions are
        /// made, and an <see cref="Endpoint"/> is associated with the <see cref="HttpContext"/>. The <see cref="EndpointMiddleware"/>
        /// defines a point in the middleware pipeline where the current <see cref="Endpoint"/> is executed. Middleware between
        /// the <see cref="EndpointRoutingMiddleware"/> and <see cref="EndpointMiddleware"/> may observe or change the
        /// <see cref="Endpoint"/> associated with the <see cref="HttpContext"/>.
        /// </para>
        /// </remarks>
        public static IApplicationBuilder UseLocalizedRouting(this IApplicationBuilder app, IApplicationContext appContext)
        {
            // PRE routing
            if (appContext.IsInstalled)
            {
                app.Use(OnBeforeRouting);
            }

            // Actual routing middleware
            app.UseRouting();

            // POST routing
            if (appContext.IsInstalled)
            {
                app.Use(OnAfterRouting);
            }

            return app;
        }

        private static async Task OnBeforeRouting(HttpContext context, Func<Task> next)
        {
            var policy = new UrlPolicy(context);

            context.SetUrlPolicy(policy);

            if (policy.IsLocalizedUrl && policy.LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                // The current request URL is prefixed by a culture code (e.g. "en")
                // that we must strip off. Otherwise the original routing middleware
                // won't find any matching endpoint.
                context.Request.Path = policy.Path.Value.EnsureStartsWith('/');
            }

            await next();
        }

        private static async Task OnAfterRouting(HttpContext context, Func<Task> next)
        {
            var policy = context.GetUrlPolicy();
            var isLocalizedUrl = policy.IsLocalizedUrl && policy.LocalizationSettings.SeoFriendlyUrlsForLanguagesEnabled;
            var routeValues = context.Features.Get<IRouteValuesFeature>()?.RouteValues;

            policy.IsSlugRoute = routeValues?.ContainsKey(SlugRouteTransformer.UrlRecordRouteKey) == true;

            var endpoint = context.GetEndpoint();
            var isLocalizedEndpoint = policy.IsSlugRoute || endpoint?.Metadata?.GetMetadata<ILocalizedRoute>() != null;

            // Restore request path to its original state.
            context.Request.Path = policy.OriginalPath;

            if (isLocalizedUrl != isLocalizedEndpoint)
            {
                var canOmitCultureCode =
                    (!policy.Culture.HasValue || policy.Culture.Value == policy.DefaultCultureCode) &&
                    policy.LocalizationSettings.DefaultLanguageRedirectBehaviour == DefaultLanguageRedirectBehaviour.StripSeoCode;

                if (isLocalizedUrl || !canOmitCultureCode)
                {
                    context.SetEndpoint(null);
                }
            }

            if (policy.Culture.HasValue && routeValues != null)
            {
                routeValues["culture"] = policy.Culture.Value;
            }

            await next();
        }
    }
}
