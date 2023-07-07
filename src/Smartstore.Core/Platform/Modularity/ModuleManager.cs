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
    /// Cached provider brand image URLs.
    /// Brand images are located in the "wwwroot/brands" module subfolder.
    /// </summary>
    public class ProviderBrandImage
    {
        /// <summary>
        /// Gets the fully qualified app relative path to the provider's default
        /// brand image. The search pattern is:
        /// "{SysName}.png", "{SysName}.gif", "{SysName}.jpg", "default.png", "default.gif", "default.jpg".
        /// If no file is found, then the parent descriptor's 
        /// <see cref="IModuleDescriptor.BrandImageFileName"/> will be returned instead.
        /// </summary>
        public string DefaultImageUrl { get; set; }

        /// <summary>
        /// Gets the fully qualified app relative paths to the provider's numbered brand images,
        /// e.g. "provider-1.png", "provider-2.png" etc. Up to 5 images are allowed.
        /// </summary>
        public string[] NumberedImageUrls { get; set; }
    }

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
            var result = _locService.GetResource(resourceName, languageId, logIfNotFound: false, returnEmptyIfNotFound: true);

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

        public string GetLocalizedValue<TMetadata>(
            TMetadata metadata,
            Expression<Func<TMetadata, string>> keySelector,
            int languageId = 0,
            bool returnDefaultValue = true)
            where TMetadata : IProviderMetadata
        {
            Guard.NotNull(metadata);
            Guard.NotNull(keySelector);

            var invoker = keySelector.GetPropertyInvoker();
            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, invoker.Property.Name);
            // INFO: " " instead of "" --> hackish approach to overcome the limitation
            // that we don't have a fallbackToMaster parameter. I don't want to change interface
            // signatures right now.
            var result = _locService.GetResource(resourceName, languageId, false, " ", true).Trim();

            if (returnDefaultValue && string.IsNullOrEmpty(result))
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
        /// Gets a cached instance of the <see cref="ProviderBrandImage"/> class
        /// containing URLs to the resolved provider brand images 
        /// in the "wwwroot/brands" directory.
        /// </summary>
        public ProviderBrandImage GetBrandImage(ProviderMetadata metadata)
        {
            var descriptor = metadata.ModuleDescriptor;
            
            if (descriptor != null)
            {
                var systemName = metadata.SystemName.ToLower();
                var cacheKey = $"ProviderBrandImage.{systemName}";

                return _cache.Get(cacheKey, o => 
                {
                    o.ExpiresIn(TimeSpan.FromDays(1));
                    
                    var result = new ProviderBrandImage();
                    var extensions = new[] { "png", "gif", "jpg" };
                    var fs = descriptor.WebRoot as IFileSystem;

                    // Find [systemName].[ext]
                    if (TryFindFile(systemName, out var defaultImageUrl))
                    {
                        result.DefaultImageUrl = defaultImageUrl;
                    }
                    // Find default.[ext]
                    else if (TryFindFile("default", out defaultImageUrl))
                    {
                        result.DefaultImageUrl = defaultImageUrl;
                    }

                    if (defaultImageUrl == null)
                    {
                        // No default image found, take fallback.
                        if (metadata.GroupName == "Payment")
                        {
                            result.DefaultImageUrl = WebHelper.ToAppRelativePath("images/default-payment-icon.png");
                        }
                        else if (descriptor.BrandImageFileName.HasValue())
                        {
                            result.DefaultImageUrl = WebHelper.ToAppRelativePath(PathUtility.Combine(descriptor.Path, descriptor.BrandImageFileName));
                        }
                    }

                    // Payment methods like credit card can have multiple brands like
                    // Master Card, Visa, etc. 5 Icons per provider should be enough.
                    var numberedImages = new List<string>(5);
                    for (var i = 1; i <= 5; i++)
                    {
                        // Find [systemName]-[i].[ext]
                        if (!TryFindFile($"{systemName}-{i}", out var numberedImageUrl))
                        {
                            break;
                        }
                        else
                        {
                            numberedImages.Add(numberedImageUrl);
                        }
                    }

                    if (result.DefaultImageUrl == null && numberedImages.Count > 0)
                    {
                        result.DefaultImageUrl = numberedImages[0];
                    }

                    result.NumberedImageUrls = numberedImages.ToArray();

                    return result;

                    bool TryFindFile(string name, out string url)
                    {
                        url = null;

                        foreach (var ext in extensions)
                        {
                            var subpath = PathUtility.Combine("brands", $"{name}.{ext}");
                            if (fs.FileExists(subpath))
                            {
                                url = WebHelper.ToAppRelativePath(PathUtility.Combine(descriptor.Path, subpath));
                                return true;
                            }
                        }

                        return false;
                    }
                });
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
