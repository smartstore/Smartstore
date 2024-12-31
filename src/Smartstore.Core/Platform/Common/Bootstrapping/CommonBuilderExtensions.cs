using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Security;

namespace Smartstore.Core.Bootstrapping
{
    public static class CommonBuilderExtensions
    {
        public static IServiceCollection AddWorkContext(this IServiceCollection services, IApplicationContext appContext)
        {
            Guard.NotNull(services);
            Guard.NotNull(appContext);

            services.AddScoped<IWorkContext, DefaultWorkContext>();

            if (appContext.IsInstalled)
            {
                services.AddScoped<IWorkContextSource, DefaultWorkContextSource>();
            }
            else
            {
                services.AddScoped<IWorkContextSource, InstallationWorkContextSource>();
            }

            return services;
        }
        
        /// <summary>
        /// Initializes work context so that context data can be safely access from here on.
        /// </summary>
        public static IApplicationBuilder UseWorkContext(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                try
                {
                    var workContext = context.RequestServices.GetRequiredService<IWorkContext>();
                    await workContext.InitializeAsync();
                    await next();
                }
                catch (HttpResponseException ex)
                {
                    context.Response.StatusCode = ex.StatusCode;
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        await context.Response.WriteAsync(ex.Message);
                    }
                }
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
                context.Response.Headers.XPoweredBy = $"Smartstore {SmartstoreVersion.CurrentVersion}";
                await next(context);
            });
        }

        /// <summary>
        /// Adds the "Content-Security-Policy" HTTP header or "Content-Security-Policy-Report-Only"
        /// if <see cref="SmartConfiguration.ContentSecurityPolicyConfiguration.Report"/> is set to <c>true</c>.
        /// </summary>
        public static IApplicationBuilder UseContentSecurityHeaders(this IApplicationBuilder app, IApplicationContext appContext)
        {
            return app.Use(async (context, next) =>
            {
                var policy = appContext.AppConfiguration.ContentSecurityPolicy;
                if (policy?.Enabled ?? false)
                {
                    if (policy.Report)
                    {
                        context.Response.Headers.ContentSecurityPolicyReportOnly = policy.ToString();
                    }
                    else
                    {
                        context.Response.Headers.ContentSecurityPolicy = policy.ToString();
                    }
                }

                await next(context);
            });
        }
    }
}
