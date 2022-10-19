using Microsoft.AspNetCore.Builder;

namespace Smartstore.Web.Bootstrapping
{
    public static class WorkContextBootstrappingExtensions
    {
        public static IServiceCollection AddWorkContext(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));
            services.AddScoped<IWorkContext, WebWorkContext>();
            return services;
        }

        public static IApplicationBuilder UseWorkContext(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var workContext = context.RequestServices.GetRequiredService<IWorkContext>();
                await workContext.InitializeAsync();
                await next();
            });
        }
    }
}
