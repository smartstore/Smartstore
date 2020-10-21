using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Web;
using Autofac;
using System;
using Smartstore.Diagnostics;

namespace Smartstore.Core
{
    public class CommonServices : ICommonServices
    {
        private readonly IComponentContext _container;
        private readonly Lazy<IApplicationContext> _appContext;
        private readonly Lazy<ICacheManager> _cacheManager;
        private readonly Lazy<IRequestCache> _requestCache;
        private readonly Lazy<SmartDbContext> _dbContext;
        private readonly Lazy<IStoreContext> _storeContext;
        private readonly Lazy<IWebHelper> _webHelper;
        //private readonly Lazy<IWorkContext> _workContext;
        private readonly Lazy<IEventPublisher> _eventPublisher;
        //private readonly Lazy<ILocalizationService> _localization;
        //private readonly Lazy<ICustomerActivityService> _customerActivity;
        //private readonly Lazy<IMediaService> _mediaService;
        //private readonly Lazy<INotifier> _notifier;
        //private readonly Lazy<IPermissionService> _permissions;
        private readonly Lazy<ISettingService> _settings;
        private readonly Lazy<ISettingFactory> _settingFactory;
        //private readonly Lazy<IStoreService> _storeService;
        //private readonly Lazy<IDateTimeHelper> _dateTimeHelper;
        //private readonly Lazy<IDisplayControl> _displayControl;
        private readonly Lazy<IChronometer> _chronometer;
        //private readonly Lazy<IMessageFactory> _messageFactory;

        public CommonServices(
            IComponentContext container,
            Lazy<IApplicationContext> appContext,
            Lazy<ICacheManager> cacheManager,
            Lazy<IRequestCache> requestCache,
            Lazy<SmartDbContext> dbContext,
            Lazy<IStoreContext> storeContext,
            Lazy<IWebHelper> webHelper,
            Lazy<IEventPublisher> eventPublisher,
            Lazy<ISettingService> settings,
            Lazy<ISettingFactory> settingFactory,
            Lazy<IChronometer> chronometer)
        {
            _container = container;
            _appContext = appContext;
            _cacheManager = cacheManager;
            _requestCache = requestCache;
            _dbContext = dbContext;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _eventPublisher = eventPublisher;
            _settings = settings;
            _settingFactory = settingFactory;
            _chronometer = chronometer;
        }

        public IComponentContext Container => _container;
        public IApplicationContext ApplicationContext => _appContext.Value;
        public ICacheManager Cache => _cacheManager.Value;
        public IRequestCache RequestCache => _requestCache.Value;
        public SmartDbContext DbContext => _dbContext.Value;
        public IStoreContext StoreContext => _storeContext.Value;
        public IWebHelper WebHelper => _webHelper.Value;
        public IEventPublisher EventPublisher => _eventPublisher.Value;
        public ISettingService Settings => _settings.Value;
        public ISettingFactory SettingFactory => _settingFactory.Value;
        public IChronometer Chronometer => _chronometer.Value;
    }
}
