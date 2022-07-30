using Autofac.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore;

namespace Autofac
{
    public static class IRegistrationBuilderExtensions
    {
        /// <summary>
        /// Tries to determine the component's lifetime by first checking for the
        /// <see cref="ServiceLifetimeAttribute"/> on the implementation type 
        /// or using <paramref name="fallback"/> if the attribute is not defined.
        /// </summary>
        public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle>
            InstancePerAttributedLifetime<TLimit, TActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> registration,
            ServiceLifetime fallback = ServiceLifetime.Scoped)
            where TActivatorData : ReflectionActivatorData
        {
            Guard.NotNull(registration, nameof(registration));

            var activatorData = registration.ActivatorData;
            var lifetime = activatorData.ImplementationType.GetAttribute<ServiceLifetimeAttribute>(false)?.Lifetime ?? fallback;

            if (lifetime == ServiceLifetime.Singleton)
            {
                registration.SingleInstance();
            }
            else if (lifetime == ServiceLifetime.Transient)
            {
                registration.InstancePerDependency();
            }
            else
            {
                registration.InstancePerLifetimeScope();
            }

            return registration;
        }
    }
}
