using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// A mediator between modules/providers and core application services: 
    /// provides localization, setting access, module instantiation etc.
    /// </summary>
    public partial class ModuleManager
    {
        private readonly IApplicationContext _appContext;
        private readonly SmartDbContext _db;
        private readonly ILocalizationService _locService;
        private readonly ISettingService _settingService;
        private readonly IWidgetService _widgetService;
        private readonly Func<string, IModule> _moduleByNameFactory;
        private readonly Func<IModuleDescriptor, IModule> _moduleByDescriptorFactory;

        public ModuleManager(
            IApplicationContext appContext,
            SmartDbContext db,
            ILocalizationService locService,
            ISettingService settingService,
            IWidgetService widgetService,
            Func<string, IModule> moduleByNameFactory,
            Func<IModuleDescriptor, IModule> moduleByDescriptorFactory)
        {
            _appContext = appContext;
            _db = db;
            _locService = locService;
            _settingService = settingService;
            _widgetService = widgetService;
            _moduleByNameFactory = moduleByNameFactory;
            _moduleByDescriptorFactory = moduleByDescriptorFactory;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Modules

        /// <summary>
        /// Creates a transient instance of a module entry class by name.
        /// </summary>
        public IModule CreateInstance(string systemName)
            => _moduleByNameFactory(systemName);

        /// <summary>
        /// Creates a transient instance of a module entry class by descriptor.
        /// </summary>
        public IModule CreateInstance(IModuleDescriptor descriptor)
            => _moduleByDescriptorFactory(descriptor);

        public string GetLocalizedFriendlyName(IModuleDescriptor descriptor, int languageId = 0, bool returnDefaultValue = true)
        {
            return GetLocalizedValue(descriptor, x => x.FriendlyName, languageId, returnDefaultValue);
        }

        public string GetLocalizedDescription(IModuleDescriptor descriptor, int languageId = 0, bool returnDefaultValue = true)
        {
            return GetLocalizedValue(descriptor, x => x.Description, languageId, returnDefaultValue);
        }

        public string GetLocalizedValue(
            IModuleDescriptor descriptor,
            Expression<Func<IModuleDescriptor, string>> keySelector,
            int languageId = 0,
            bool returnDefaultValue = true)
        {
            Guard.NotNull(descriptor, nameof(descriptor));
            Guard.NotNull(keySelector, nameof(keySelector));

            var invoker = keySelector.GetPropertyInvoker();
            var resourceName = string.Format("Plugins.{0}.{1}", invoker.Property.Name, descriptor.SystemName);
            var result = _locService.GetResource(resourceName, languageId, false, string.Empty, true);

            if (returnDefaultValue && result.IsEmpty())
            {
                result = invoker.Invoke(descriptor);
            }

            return result;
        }

        /// <summary>
        /// Returns the absolute path of a module/provider icon
        /// </summary>
        /// <param name="descriptor">The plugin descriptor. Used to resolve the physical path</param>
        /// <param name="providerSystemName">Optional system name of provider. If passed, an icon with this name gets being tried to resolve first.</param>
        /// <returns>The icon's absolute path</returns>
        public string GetIconUrl(IModuleDescriptor descriptor, string providerSystemName = null)
        {
            if (providerSystemName.HasValue())
            {
                var fileName = "icon-{0}.png".FormatInvariant(providerSystemName);
                var fileInfo = descriptor.WebRoot.GetFileInfo(fileName);
                if (fileInfo.Exists)
                {
                    return "~{0}/{1}".FormatInvariant(descriptor.Path, fileName);
                }
            }

            if (descriptor.WebRoot.GetFileInfo("icon.png").Exists)
            {
                return "~{0}{1}".FormatInvariant(descriptor.Path.EnsureEndsWith('/'), "icon.png");
            }
            else
            {
                return GetDefaultIconUrl(descriptor.Group);
            }
        }

        public string GetDefaultIconUrl(string groupName)
        {
            if (groupName.HasValue())
            {
                string path = "admin/images/icon-module-{0}.png".FormatInvariant(groupName.ToLower());
                if (_appContext.WebRoot.FileExists(path))
                {
                    return "~/" + path;
                }
            }

            return "~/admin/images/icon-module-default.png";
        }

        #endregion

        #region Providers

        public string GetLocalizedFriendlyName(IProviderMetadata metadata, int languageId = 0, bool returnDefaultValue = true)
        {
            return GetLocalizedValue(metadata, x => x.FriendlyName, languageId, returnDefaultValue);
        }

        public string GetLocalizedDescription(IProviderMetadata metadata, int languageId = 0, bool returnDefaultValue = true)
        {
            return GetLocalizedValue(metadata, x => x.Description, languageId, returnDefaultValue);
        }

        public string GetLocalizedValue<TMetadata>(TMetadata metadata,
            Expression<Func<TMetadata, string>> keySelector,
            int languageId = 0,
            bool returnDefaultValue = true)
            where TMetadata : IProviderMetadata
        {
            Guard.NotNull(metadata, nameof(metadata));
            Guard.NotNull(keySelector, nameof(keySelector));

            var invoker = keySelector.GetPropertyInvoker();
            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, invoker.Property.Name);
            var result = _locService.GetResource(resourceName, languageId, false, string.Empty, true);

            if (returnDefaultValue && result.IsEmpty())
            {
                result = invoker.Invoke(metadata);
            }

            return result;
        }

        public async Task ApplyLocalizedValueAsync(IProviderMetadata metadata, int languageId, string propertyName, string value)
        {
            Guard.NotNull(metadata, nameof(metadata));
            Guard.IsPositive(languageId, nameof(languageId));
            Guard.NotEmpty(propertyName, nameof(propertyName));

            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, propertyName);
            var resource = await _locService.GetLocaleStringResourceByNameAsync(resourceName, languageId, false);

            if (resource != null)
            {
                if (value.IsEmpty())
                {
                    // Delete
                    _db.LocaleStringResources.Remove(resource);
                }
                else
                {
                    // Update
                    resource.ResourceValue = value;
                }
            }
            else
            {
                if (value.HasValue())
                {
                    // Insert
                    _db.LocaleStringResources.Add(new LocaleStringResource
                    {
                        LanguageId = languageId,
                        ResourceName = resourceName,
                        ResourceValue = value,
                    });
                }
            }
        }

        public int? GetUserDisplayOrder(ProviderMetadata metadata)
        {
            return GetSetting<int?>(metadata, nameof(metadata.DisplayOrder));
        }

        public T GetSetting<T>(ProviderMetadata metadata, string propertyName)
        {
            var settingKey = metadata.SettingKeyPattern.FormatInvariant(metadata.SystemName, propertyName);
            return _settingService.GetSettingByKey<T>(settingKey);
        }

        public Task SetUserDisplayOrderAsync(ProviderMetadata metadata, int displayOrder)
        {
            Guard.NotNull(metadata, nameof(metadata));

            metadata.DisplayOrder = displayOrder;
            return ApplySettingAsync(metadata, nameof(metadata.DisplayOrder), displayOrder);
        }

        public async Task ApplySettingAsync<T>(ProviderMetadata metadata, string propertyName, T value)
        {
            Guard.NotNull(metadata, nameof(metadata));
            Guard.NotEmpty(propertyName, nameof(propertyName));

            var settingKey = metadata.SettingKeyPattern.FormatInvariant(metadata.SystemName, propertyName);

            if (value != null)
            {
                await _settingService.ApplySettingAsync(settingKey, value, 0);
            }
            else
            {
                await _settingService.RemoveSettingAsync(settingKey);
            }
        }

        public string GetBrandImageUrl(ProviderMetadata metadata)
        {
            var descriptor = metadata.ModuleDescriptor;

            if (descriptor != null)
            {
                var filesToCheck = (new string[] { "branding.{0}.png", "branding.{0}.gif", "branding.{0}.jpg", "branding.{0}.jpeg" }).Select(x => x.FormatInvariant(metadata.SystemName));
                foreach (var file in filesToCheck)
                {
                    var fileInfo = descriptor.WebRoot.GetFileInfo(file);
                    if (fileInfo.Exists)
                    {
                        return "~{0}/{1}".FormatInvariant(descriptor.Path, file);
                    }
                }

                var fallback = descriptor.BrandImageFileName;
                if (fallback.HasValue())
                {
                    return "~{0}/{1}".FormatInvariant(descriptor.Path, fallback);
                }
            }

            return null;
        }

        public string GetIconUrl(ProviderMetadata metadata)
        {
            var descriptor = metadata.ModuleDescriptor;
            return descriptor == null
                ? GetDefaultIconUrl(metadata.GroupName)
                : GetIconUrl(descriptor, metadata.SystemName);
        }

        public async Task ActivateDependentWidgetsAsync(ProviderMetadata parent, bool activate)
        {
            Guard.NotNull(parent, nameof(parent));

            if (parent.DependentWidgets == null || parent.DependentWidgets.Length == 0)
            {
                return;
            }

            foreach (var systemName in parent.DependentWidgets)
            {
                await _widgetService.ActivateWidgetAsync(systemName, activate);
            }
        }

        #endregion

    }
}
