using Microsoft.AspNetCore.Builder;

namespace Smartstore.Core.Bootstrapping
{
    public static class CommonBuilderExtensions
    {
        /// <summary>
        /// Initializes work context so that context data can be safely access from here on.
        /// </summary>
        public static IApplicationBuilder UseWorkContext(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var workContext = context.RequestServices.GetRequiredService<IWorkContext>();
                await workContext.InitializeAsync();
                await next();
            });
        }

        /// <summary>
        /// Adds X-Powered-By Smartstore HTTP header.
        /// </summary>
        public static IApplicationBuilder UsePoweredBy(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                // Add X-Powered-By header
                context.Response.Headers["X-Powered-By"] = $"Smartstore {SmartstoreVersion.CurrentVersion}";
                await next(context);
            });
        }
    }
}
