using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.Extensions.Logging;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Diagnostics;
using Smartstore.Engine;
using Smartstore.Events;

namespace Smartstore.Core
{
    public class CommonServices : ICommonServices
    {
        private readonly IComponentContext _container;
        private readonly IApplicationContext _appContext;
        private readonly ICacheFactory _cacheFactory;
        private readonly ICacheManager _cacheManager;
        private readonly IRequestCache _requestCache;
        private readonly SmartDbContext _dbContext;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILocalizationService _localization;
        private readonly Lazy<ISettingService> _settings;
        private readonly ISettingFactory _settingFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Lazy<IActivityLogger> _activityLogger;
        private readonly INotifier _notifier;
        private readonly IPermissionService _permissions;
        private readonly IChronometer _chronometer;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IMediaService _mediaService;
        private readonly Lazy<IDisplayControl> _displayControl;
        private readonly ICurrencyService _currencyService;

        public CommonServices(
            IComponentContext container,
            IApplicationContext appContext,
            ICacheFactory cacheFactory,
            ICacheManager cacheManager,
            IRequestCache requestCache,
            SmartDbContext dbContext,
            IStoreContext storeContext,
            IWorkContext workContext,
            IWebHelper webHelper,
            IEventPublisher eventPublisher,
            ILocalizationService localization,
            Lazy<ISettingService> settings,
            ISettingFactory settingFactory,
            ILoggerFactory loggerFactory,
            Lazy<IActivityLogger> activityLogger,
            INotifier notifier,
            IPermissionService permissions,
            IChronometer chronometer,
            IDateTimeHelper dateTimeHelper,
            IMediaService mediaService,
            Lazy<IDisplayControl> displayControl,
            ICurrencyService currencyService)
        {
            _container = container;
            _appContext = appContext;
            _cacheFactory = cacheFactory;
            _cacheManager = cacheManager;
            _requestCache = requestCache;
            _dbContext = dbContext;
            _storeContext = storeContext;
            _workContext = workContext;
            _webHelper = webHelper;
            _eventPublisher = eventPublisher;
            _localization = localization;
            _settings = settings;
            _settingFactory = settingFactory;
            _loggerFactory = loggerFactory;
            _activityLogger = activityLogger;
            _notifier = notifier;
            _permissions = permissions;
            _chronometer = chronometer;
            _dateTimeHelper = dateTimeHelper;
            _mediaService = mediaService;
            _displayControl = displayControl;
            _currencyService = currencyService;
        }

        public IComponentContext Container => _container;
        public IApplicationContext ApplicationContext => _appContext;
        public ICacheFactory CacheFactory => _cacheFactory;
        public ICacheManager Cache => _cacheManager;
        public IRequestCache RequestCache => _requestCache;
        public SmartDbContext DbContext => _dbContext;
        public IStoreContext StoreContext => _storeContext;
        public IWorkContext WorkContext => _workContext;
        public IWebHelper WebHelper => _webHelper;
        public IEventPublisher EventPublisher => _eventPublisher;
        public ILocalizationService Localization => _localization;
        public ISettingService Settings => _settings.Value;
        public ISettingFactory SettingFactory => _settingFactory;
        public ILoggerFactory LoggerFactory => _loggerFactory;
        public IActivityLogger ActivityLogger => _activityLogger.Value;
        public INotifier Notifier => _notifier;
        public IPermissionService Permissions => _permissions;
        public IChronometer Chronometer => _chronometer;
        public IDateTimeHelper DateTimeHelper => _dateTimeHelper;
        public IMediaService MediaService => _mediaService;
        public IDisplayControl DisplayControl => _displayControl.Value;
        public ICurrencyService CurrencyService => _currencyService;
    }

    internal class CommonServicesModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CommonServices>().As<ICommonServices>().InstancePerLifetimeScope();
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
        {
            // Look for first settable property of type "ICommonServices" and inject
            var servicesProperty = FindCommonServicesProperty(registration.Activator.LimitType);

            if (servicesProperty == null)
                return;

            registration.Metadata.Add("Property.ICommonServices", FastProperty.Create(servicesProperty));

            registration.PipelineBuilding += (sender, pipeline) =>
            {
                // Add our CommonServices middleware to the pipeline.
                pipeline.Use(PipelinePhase.ParameterSelection, (context, next) =>
                {
                    next(context);

                    if (!DataSettings.DatabaseIsInstalled())
                    {
                        return;
                    }

                    if (!context.NewInstanceActivated || context.Registration.Metadata.Get("Property.ICommonServices") is not FastProperty prop)
                    {
                        return;
                    }

                    try
                    {
                        var services = context.Resolve<ICommonServices>();
                        prop.SetValue(context.Instance, services);
                    }
                    catch { }
                });
            };
        }

        private static PropertyInfo FindCommonServicesProperty(Type type)
        {
            var prop = type
                .GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    PropertyInfo = p,
                    p.PropertyType,
                    IndexParameters = p.GetIndexParameters(),
                    Accessors = p.GetAccessors(false)
                })
                .Where(x => x.PropertyType == typeof(ICommonServices)) // must be ICommonServices
                .Where(x => x.IndexParameters.Count() == 0) // must not be an indexer
                .Where(x => x.Accessors.Length != 1 || x.Accessors[0].ReturnType == typeof(void)) //must have get/set, or only set
                .Select(x => x.PropertyInfo)
                .FirstOrDefault();

            return prop;
        }
    }
}
