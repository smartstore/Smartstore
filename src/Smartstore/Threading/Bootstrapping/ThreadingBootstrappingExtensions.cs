using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Threading;

namespace Smartstore.Bootstrapping
{
    public static class ThreadingBootstrappingExtensions
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

        public static IServiceCollection AddAsyncRunner(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));
            services.AddSingleton<AsyncRunner>();
            return services;
        }

        public static IServiceCollection AddLockFileManager(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));
            services.AddSingleton<ILockFileManager, LockFileManager>();
            return services;
        }

        public static IServiceCollection AddDistributedSemaphoreLockProvider(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));
            services.AddSingleton<IDistributedLockProvider, DistributedSemaphoreLockProvider>();
            return services;
        }
    }
}
