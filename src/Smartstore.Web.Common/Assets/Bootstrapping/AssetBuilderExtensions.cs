using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Smartstore.Web.Assets;
using WebOptimizer;

namespace Smartstore.Web.Bootstrapping
{
    public static class AssetBuilderExtensions
    {
        /// <summary>
        /// Adds asset & bundling middleware to the <see cref="IApplicationBuilder"/> request execution pipeline
        /// </summary>
        public static IApplicationBuilder UseAssets(this IApplicationBuilder app)
        {
            Guard.NotNull(app, nameof(app));

            if (app.ApplicationServices.GetService<IAssetPipeline>() == null)
            {
                string msg = "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddWebOptimizer' inside the call to 'ConfigureServices(...)' in the application startup code.";
                throw new InvalidOperationException(msg);
            }

            app.UseMiddleware<AssetMiddleware>();

            return app;
        }
    }
}
