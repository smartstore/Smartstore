using Microsoft.AspNetCore.Builder;
using Smartstore.Engine.Initialization;

namespace Smartstore.Bootstrapping
{
    public static class ApplicationInitializerExtensions
    {
        /// <summary>
        /// Executes <see cref="IApplicationInitializer"/> implementations during the very first request.
        /// </summary>
        public static IApplicationBuilder UseApplicationInitializer(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApplicationInitializerMiddleware>();
        }
    }
}