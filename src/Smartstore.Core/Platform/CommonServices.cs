using System;
using Autofac;
using Microsoft.Extensions.Logging;
using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Diagnostics;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Web;

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
        private readonly IChronometer _chronometer;
        //private readonly Lazy<IStoreService> _storeService;
        //private readonly Lazy<IDateTimeHelper> _dateTimeHelper;
        //private readonly Lazy<IDisplayControl> _displayControl;
        //private readonly Lazy<ILocalizationService> _localization;
        //private readonly Lazy<ICustomerActivityService> _customerActivity;
        //private readonly Lazy<IMediaService> _mediaService;
        //private readonly Lazy<INotifier> _notifier;
        //private readonly Lazy<IPermissionService> _permissions;
        //private readonly Lazy<IMessageFactory> _messageFactory;

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
        public IChronometer Chronometer => _chronometer;
    }
}
