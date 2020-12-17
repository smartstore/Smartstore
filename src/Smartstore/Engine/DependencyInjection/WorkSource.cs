using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Smartstore.Engine;

namespace Smartstore.DependencyInjection
{
    public class WorkSource : IRegistrationSource
    {
        static readonly MethodInfo CreateMetaRegistrationMethod = typeof(WorkSource).GetMethod(
            "CreateMetaRegistration", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool IsClosingTypeOf(Type type, Type openGenericType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == openGenericType;
        }

        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            if (service is not IServiceWithType swt || !IsClosingTypeOf(swt.ServiceType, typeof(Work<>)))
                return Enumerable.Empty<IComponentRegistration>();

            var valueType = swt.ServiceType.GetGenericArguments()[0];

            var valueService = swt.ChangeType(valueType);

            var registrationCreator = CreateMetaRegistrationMethod.MakeGenericMethod(valueType);

            return registrationAccessor(valueService)
                .Select(v => registrationCreator.Invoke(null, new object[] { service, v }))
                .Cast<IComponentRegistration>();
        }

        public bool IsAdapterForIndividualComponents => true;

        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by reflection")]
        static IComponentRegistration CreateMetaRegistration<T>(Service providedService, ServiceRegistration serviceRegistration) where T : class
        {
            var rb = RegistrationBuilder.ForDelegate(
                (c, p) =>
                {
                    var accessor = c.Resolve<ILifetimeScopeAccessor>();
                    return new Work<T>(w =>
                    {
                        var scope = accessor.LifetimeScope;
                        if (scope == null)
                            return default;

                        var workValues = scope.Resolve<WorkValues<T>>();

                        if (!workValues.Values.TryGetValue(w, out T value))
                        {
                            var request = new ResolveRequest(providedService, serviceRegistration, p);
                            value = (T)workValues.ComponentContext.ResolveComponent(request);
                            workValues.Values[w] = value;
                        }

                        return value;
                    });
                })
                .As(providedService)
                .Targeting(serviceRegistration.Registration)
                .SingleInstance();

            return rb.CreateRegistration();
        }
    }

    public class WorkValues<T> where T : class
    {
        public WorkValues(IComponentContext componentContext)
        {
            ComponentContext = componentContext;
            Values = new Dictionary<Work<T>, T>();
        }

        public IComponentContext ComponentContext { get; private set; }
        public IDictionary<Work<T>, T> Values { get; private set; }
    }
}
