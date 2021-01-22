using Microsoft.AspNetCore.Builder;
using Smartstore.Threading;

namespace Smartstore.Bootstrapping
{
    public static class ThreadingBuilderExtensions
    {
        /// <summary>
        /// Starts async flow of <see cref="ContextState"/>. Should be registered very early in the pipeline
        /// so that inner async scopes can propagate data to outer scopes.
        /// </summary>
        public static IApplicationBuilder UseContextState(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                ContextState.StartAsyncFlow();
                await next();
            });
        }
    }
}
