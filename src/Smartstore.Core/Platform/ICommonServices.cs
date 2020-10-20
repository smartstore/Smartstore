using Autofac;
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
        IWebHelper WebHelper { get; }
        IEventPublisher EventPublisher { get; }
        ISettingService Settings { get; }

        // TODO: (core) Add more props to ICommonServices once they drop in.
    }
}
