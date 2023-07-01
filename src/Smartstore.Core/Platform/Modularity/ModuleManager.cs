using Smartstore.Caching;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;
using Smartstore.Http;
using Smartstore.IO;

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
        private readonly ICacheManager _cache;
        private readonly ILocalizationService _locService;
        private readonly ISettingService _settingService;
        private readonly IWidgetService _widgetService;
        private readonly Func<string, IModule> _moduleByNameFactory;
        private readonly Func<IModuleDescriptor, IModule> _moduleByDescriptorFactory;

        public ModuleManager(
            IApplicationContext appContext,
            SmartDbContext db,
            ICacheManager cache,
            ILocalizationService locService,
            ISettingService settingService,
            IWidgetService widgetService,
            Func<string, IModule> moduleByNameFactory,
            Func<IModuleDescriptor, IModule> moduleByDescriptorFactory)
        {
            _appContext = appContext;
            _db = db;
            _cache = cache;
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
            Guard.NotNull(descriptor);
            Guard.NotNull(keySelector);

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
        /// <param name="providerSystemName">Optional system name of provider. If passed, an icon with this name is being tried to resolve first.</param>
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
            Guard.NotNull(metadata);
            Guard.NotNull(keySelector);

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
            Guard.NotNull(metadata);
            Guard.IsPositive(languageId);
            Guard.NotEmpty(propertyName);

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
            Guard.NotNull(metadata);

            metadata.DisplayOrder = displayOrder;
            return ApplySettingAsync(metadata, nameof(metadata.DisplayOrder), displayOrder);
        }

        public async Task ApplySettingAsync<T>(ProviderMetadata metadata, string propertyName, T value)
        {
            Guard.NotNull(metadata);
            Guard.NotEmpty(propertyName);

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

        /// <summary>
        /// Gets the fully qualified app relative path to a provider's default
        /// brand image which is located in the module's <c>wwwroot/brands</c> directory.
        /// The search pattern is:
        /// "{SysName}.png", "{SysName}.gif", "{SysName}.jpg", "default.png", "default.gif", "default.jpg".
        /// If no file is found, then the parent descriptor's 
        /// <see cref="IModuleDescriptor.BrandImageFileName"/> will be returned instead.
        /// </summary>
        /// <remarks>
        /// The resolution result is cached.
        /// </remarks>
        public string GetDefaultBrandImageUrl(ProviderMetadata metadata)
        {
            var descriptor = metadata.ModuleDescriptor;
            
            if (descriptor != null)
            {
                var systemName = metadata.SystemName.ToLower();
                var cacheKey = $"DefaultBrandImageUrl.{systemName}";

                return _cache.Get(cacheKey, () => 
                {
                    // Check provider specific icons.
                    var filesToCheck = new List<string> { "{0}.png", "{0}.gif", "{0}.jpg", "default.png", "default.gif", "default.jpg" }
                        .Select(x => x.FormatInvariant(systemName))
                        .ToList();

                    var fs = descriptor.WebRoot as IFileSystem;
                    foreach (var file in filesToCheck)
                    {
                        if (fs.FileExists("brands/" + file))
                        {
                            return WebHelper.ToAppRelativePath(PathUtility.Combine(descriptor.Path, "brands", file));
                        }
                    }

                    if (metadata.GroupName == "Payment")
                    {
                        return WebHelper.ToAppRelativePath("images/default-payment-icon.png");
                    }

                    // Try to find fallback icon branding.png.
                    if (descriptor.BrandImageFileName.HasValue())
                    {
                        return WebHelper.ToAppRelativePath(PathUtility.Combine(descriptor.Path, descriptor.BrandImageFileName));
                    }

                    return string.Empty;
                });
            }

            return string.Empty;
        }

        public string[] GetBrandImageUrls(ProviderMetadata metadata)
        {
            // TODO: continue...
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
            Guard.NotNull(parent);

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
