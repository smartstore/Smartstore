using System;
using Autofac;
using Microsoft.Extensions.Logging;
using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Logging;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Diagnostics;
using Smartstore.Engine;
using Smartstore.Events;

namespace Smartstore.Core
{
    public class CommonServices : ICommonServices
    {
        private readonly IComponentContext _container;
        private readonly IApplicationContext _appContext;
        private readonly ICacheManager _cacheManager;
        private readonly IRequestCache _requestCache;
        private readonly SmartDbContext _dbContext;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly Lazy<ISettingService> _settings;
        private readonly ISettingFactory _settingFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Lazy<IActivityLogger> _activityLogger;
        private readonly INotifier _notifier;
        private readonly IChronometer _chronometer;

        public CommonServices(
            IComponentContext container,
            IApplicationContext appContext,
            ICacheManager cacheManager,
            IRequestCache requestCache,
            SmartDbContext dbContext,
            IStoreContext storeContext,
            IWorkContext workContext,
            IWebHelper webHelper,
            IEventPublisher eventPublisher,
            Lazy<ISettingService> settings,
            ISettingFactory settingFactory,
            ILoggerFactory loggerFactory,
            Lazy<IActivityLogger> activityLogger,
            INotifier notifier,
            IChronometer chronometer)
        {
            _container = container;
            _appContext = appContext;
            _cacheManager = cacheManager;
            _requestCache = requestCache;
            _dbContext = dbContext;
            _storeContext = storeContext;
            _workContext = workContext;
            _webHelper = webHelper;
            _eventPublisher = eventPublisher;
            _settings = settings;
            _settingFactory = settingFactory;
            _loggerFactory = loggerFactory;
            _activityLogger = activityLogger;
            _notifier = notifier;
            _chronometer = chronometer;
        }

        public IComponentContext Container => _container;
        public IApplicationContext ApplicationContext => _appContext;
        public ICacheManager Cache => _cacheManager;
        public IRequestCache RequestCache => _requestCache;
        public SmartDbContext DbContext => _dbContext;
        public IStoreContext StoreContext => _storeContext;
        public IWorkContext WorkContext => _workContext;
        public IWebHelper WebHelper => _webHelper;
        public IEventPublisher EventPublisher => _eventPublisher;
        public ISettingService Settings => _settings.Value;
        public ISettingFactory SettingFactory => _settingFactory;
        public ILoggerFactory LoggerFactory => _loggerFactory;
        public IActivityLogger ActivityLogger => _activityLogger.Value;
        public INotifier Notifier => _notifier;
        public IChronometer Chronometer => _chronometer;
    }
}
