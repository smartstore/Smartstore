using Microsoft.Extensions.DependencyInjection;
using Smartstore.Threading;

namespace Smartstore.Bootstrapping
{
    public static class ThreadingServiceCollectionExtensions
    {
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
    }
}
