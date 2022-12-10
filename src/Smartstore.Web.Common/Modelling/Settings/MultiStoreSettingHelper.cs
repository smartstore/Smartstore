using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;

namespace Smartstore.Web.Modelling.Settings
{
    public class MultiStoreSettingHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IViewDataAccessor _viewDataAccessor;
        private readonly SmartDbContext _db;
        private readonly ISettingService _settingService;
        private readonly ILocalizedEntityService _leService;

        public MultiStoreSettingHelper(
            IHttpContextAccessor httpContextAccessor,
            IViewDataAccessor viewDataAccessor,
            SmartDbContext db,
            ISettingService settingService,
            ILocalizedEntityService leService)
        {
            _httpContextAccessor = httpContextAccessor;
            _viewDataAccessor = viewDataAccessor;
            _db = db;
            _settingService = settingService;
            _leService = leService;
        }

        public static string ViewDataKey => "MultiStoreSettingData";

        public ViewDataDictionary ViewData
        {
            get => _viewDataAccessor.ViewData;
        }

        public MultiStoreSettingData Data
        {
            get => ViewData[ViewDataKey] as MultiStoreSettingData;
            set => ViewData[ViewDataKey] = Guard.NotNull(value);
        }

        private MultiStoreSettingData GetOrCreateData(int storeScope)
        {
            Guard.IsPositive(storeScope);

            var data = Data;
            if (data != null && data.ActiveStoreScopeConfiguration > 0 && data.ActiveStoreScopeConfiguration != storeScope)
            {
                throw new InvalidOperationException($"The passed storeScope must match the scope of the current request's scope. Expected: {storeScope}, was: {data.ActiveStoreScopeConfiguration}.");
            }

            data = Data ??= new MultiStoreSettingData { ActiveStoreScopeConfiguration = storeScope };

            return data;
        }

        internal async Task<string> FindOverridenSettingKey(string prefix, string name, bool isRootModel, bool allowEmpty, Func<string, Task<string>> storeAccessor)
        {
            var request = _httpContextAccessor.HttpContext?.Request;

            if (!request.HasFormContentType || request.Form == null)
            {
                // This is a GET operation (no form posted), so check against storage
                var key = prefix + '.' + name;
                var storedValue = await storeAccessor(key);
                var overridden = allowEmpty ? storedValue != null : storedValue.HasValue();
                if (overridden)
                {
                    return isRootModel ? name : key;
                }
            }
            else
            {
                // A POST operation. Only check form.
                if (IsOverrideChecked(prefix, name, request.Form, out var key))
                {
                    return key;
                }
            }

            return null;
        }

        public static bool IsOverrideChecked<TSetting>(TSetting settingInstance, string name, IFormCollection form) 
            where TSetting : ISettings
            => IsOverrideChecked(Guard.NotNull(settingInstance).GetType(), name, form);

        public static bool IsOverrideChecked(Type settingType, string name, IFormCollection form)
        {
            Guard.NotNull(settingType, nameof(settingType));
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(form, nameof(form));

            return IsOverrideChecked(settingType.Name, name, form, out _);
        }

        internal static bool IsOverrideChecked(string prefix, string name, IFormCollection form, out string key)
        {
            if (prefix.HasValue())
            {
                key = prefix + '.' + name;
                if (IsOverridden(key, form))
                {
                    return true;
                }
            }

            key = name;
            if (IsOverridden(key, form))
            {
                return true;
            }

            key = null;
            return false;

            static bool IsOverridden(string key, IFormCollection form)
            {
                key += "_OverrideForStore";

                if (form.ContainsKey(key))
                {
                    var checkboxValue = form[key].FirstOrDefault().EmptyNull().ToLower();
                    return checkboxValue.Contains("on") || checkboxValue.Contains("true");
                }

                return false;
            }
        }

        public void AddOverrideKey(object settings, string name, int storeScope)
        {
            var data = GetOrCreateData(storeScope);
            var key = (settings?.GetType()?.Name.EmptyNull() + '.' + name).Trim('.');
            data.OverrideSettingKeys.Add(key);
        }

        public Task DetectOverrideKeysAsync(
            object settings,
            object model,
            int storeId,
            bool isRootModel = true,
            Func<string, string> propertyNameMapper = null)
        {
            return DetectOverrideKeysInternal(settings, model, storeId, isRootModel, propertyNameMapper, null);
        }

        private async Task DetectOverrideKeysInternal(
            object settings,
            object model,
            int storeId,
            bool isRootModel,
            Func<string, string> propertyNameMapper,
            int? localeIndex)
        {
            if (storeId <= 0)
            {
                // Single store mode -> there are no overrides.
                return;
            }

            var fieldPrefix = ViewData?.TemplateInfo?.HtmlFieldPrefix.NullEmpty();
            var settingType = settings.GetType();
            var settingName = settingType.Name;
            var modelType = model.GetType();
            var modelProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var localizedModelLocal = model as ILocalizedLocaleModel;
            var settingPrefix = fieldPrefix ?? settingName;
            var data = GetOrCreateData(storeId);

            foreach (var prop in modelProperties)
            {
                string key = null;
                var name = propertyNameMapper?.Invoke(prop.Name) ?? prop.Name;
                var settingProperty = settingType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

                if (settingProperty == null)
                {
                    // Setting is not configurable or missing or whatever... however we don't need the override info.
                    continue;
                }

                if (localizedModelLocal == null)
                {
                    key = await FindOverridenSettingKey(
                        settingPrefix, 
                        name,
                        isRootModel: isRootModel,
                        allowEmpty: true, 
                        storeAccessor: x => _settingService.GetSettingByKeyAsync<string>(x, storeId: storeId));
                }
                else if (localeIndex.HasValue)
                {
                    var localeKey = $"Locales[{localeIndex.Value}].{name}";
                    key = await FindOverridenSettingKey(
                        settingPrefix, 
                        localeKey,
                        isRootModel: isRootModel,
                        allowEmpty: true,
                        storeAccessor: x => _leService.GetLocalizedValueAsync(localizedModelLocal.LanguageId, storeId, settingName, name));
                }

                if (key != null)
                {
                    data.OverrideSettingKeys.Add(key);
                }
            }

            if (model is ILocalizedModel)
            {
                var localesProperty = modelType.GetProperty("Locales", BindingFlags.Public | BindingFlags.Instance);
                if (localesProperty != null)
                {
                    if (localesProperty.GetValue(model) is IEnumerable<ILocalizedLocaleModel> locales)
                    {
                        int i = 0;
                        foreach (var locale in locales)
                        {
                            await DetectOverrideKeysInternal(settings, locale, storeId, false, propertyNameMapper, i);
                            i++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the form key of the control to the list of override setting keys which are used to determine which settings are overriden on store level.
        /// </summary>
        /// <param name="formKey">The key of the input element that represents the control.</param>
        /// <param name="settingName">Name of the setting (will be concatenated with name of settings seperated by dot e.g. SocalSettings.Facebook)</param>
        /// <param name="settings">Settings instance which contains the particular setting (will be concatenated with name of settings seperated by dot e.g. SocalSettings.Facebook)</param>
        /// <param name="storeId">Id of the configured store dependency.</param>
        public async Task DetectOverrideKeyAsync(string formKey, string settingName, object settings, int storeId)
        {
            await DetectOverrideKeyAsync(formKey, settings.GetType().Name + "." + settingName, storeId);
        }

        /// <summary>
        /// Adds the form key of the control to the list of override setting keys which are used to determine which settings are overriden on store level.
        /// </summary>
        /// <param name="formKey">The key of the input element that represents the control.</param>
        /// <param name="fullSettingName">Fully qualified name of the setting (e.g. SocalSettings.Facebook)</param>
        /// <param name="storeId">Id of the configured store dependency.</param>
        public async Task DetectOverrideKeyAsync(string formKey, string fullSettingName, int storeId)
        {
            if (storeId <= 0)
            {
                // Single store mode -> there are no overrides.
                return;
            }

            var key = formKey;
            if (await _settingService.GetSettingByKeyAsync<string>(fullSettingName, storeId: storeId) == null)
            {
                key = null;
            }

            if (key != null)
            {
                var data = GetOrCreateData(storeId);
                data.OverrideSettingKeys.Add(key);
            }
        }

        /// <summary>
        /// Updates settings for a store.
        /// </summary>
        /// <param name="settings">Settings class instance.</param>
        /// <param name="form">Form value collection.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <param name="settingService">Setting service.</param>
        /// <param name="propertyNameMapper">Function to map property names. Return <c>null</c> to skip a property.</param>
        public async Task UpdateSettingsAsync(
            object settings,
            IFormCollection form,
            int storeId,
            Func<string, string> propertyNameMapper = null)
        {
            var settingType = settings.GetType();
            var settingName = settingType.Name;
            var settingProperties = FastProperty.GetProperties(settingType).Values;

            foreach (var prop in settingProperties)
            {
                var name = propertyNameMapper?.Invoke(prop.Name) ?? prop.Name;

                if (name.IsEmpty())
                {
                    continue;
                }

                var key = settingName + "." + name;

                if (storeId == 0 || IsOverrideChecked(settingName, name, form, out _))
                {
                    dynamic value = prop.GetValue(settings);
                    await _settingService.ApplySettingAsync(key, value ?? string.Empty, storeId);
                }
                else if (storeId > 0)
                {
                    await _settingService.RemoveSettingAsync(key, storeId);
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task ApplySettingAsync(
            string formKey,
            string settingName,
            object settings,
            IFormCollection form,
            int storeId)
        {
            var settingType = settings.GetType();

            if (storeId == 0 || IsOverrideChecked(null, formKey, form, out _))
            {
                var prop = settingType.GetProperty(settingName);
                if (prop != null)
                {
                    dynamic value = prop.GetValue(settings);
                    var key = settingType.Name + "." + settingName;
                    await _settingService.ApplySettingAsync(key, value ?? string.Empty, storeId);
                }
            }
            else if (storeId > 0)
            {
                var key = settingType.Name + "." + settingName;
                await _settingService.RemoveSettingAsync(key, storeId);
            }
        }
    }
}
