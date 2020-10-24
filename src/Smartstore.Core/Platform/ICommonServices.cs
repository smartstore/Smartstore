using System;
using Autofac;
using Microsoft.Extensions.Logging;
using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Web;

namespace Smartstore.Core
{
    public interface ICommonServices
    {
        IComponentContext Container { get; }
        IApplicationContext ApplicationContext { get; }
        ICacheManager Cache { get; }
        IRequestCache RequestCache { get; }
        SmartDbContext DbContext { get; }
        IStoreContext StoreContext { get; }
        IWorkContext WorkContext { get; }
        IWebHelper WebHelper { get; }
        IEventPublisher EventPublisher { get; }
        ISettingService Settings { get; }
        ISettingFactory SettingFactory { get; }
        ILoggerFactory LoggerFactory { get; }

        // TODO: (core) Add more props to ICommonServices once they drop in.
    }

    public static class ICommonServicesExtensions
    {
        public static TService Resolve<TService>(this ICommonServices services)
        {
            return services.Container.Resolve<TService>();
        }

        public static TService ResolveKeyed<TService>(this ICommonServices services, object serviceKey)
        {
            return services.Container.ResolveKeyed<TService>(serviceKey);
        }

        public static TService ResolveNamed<TService>(this ICommonServices services, string serviceName)
        {
            return services.Container.ResolveNamed<TService>(serviceName);
        }
    }
}
