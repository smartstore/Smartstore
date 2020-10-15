using Smartstore;
using Smartstore.Events;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all dependencies required for the pub/sub system (without discovering <see cref="IConsumer"/> implementations)
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddEventPublisher(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));

            services.AddSingleton<IMessageBus, NullMessageBus>();
            services.AddSingleton<IEventPublisher, EventPublisher>();
            services.AddSingleton<IConsumerRegistry, ConsumerRegistry>();
            services.AddSingleton<IConsumerResolver, ConsumerResolver>();
            services.AddSingleton<IConsumerInvoker, ConsumerInvoker>();

            return services;
        }
    }
}
