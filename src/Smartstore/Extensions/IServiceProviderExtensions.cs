using System;
using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace Smartstore
{
    public static class IServiceProviderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifetimeScope AsLifetimeScope(this IServiceProvider serviceProvider)
            => serviceProvider.GetAutofacRoot();
    }
}
