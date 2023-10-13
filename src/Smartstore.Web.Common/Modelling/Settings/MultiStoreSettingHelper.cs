using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;

namespace Smartstore.Web.Modelling.Settings
{
    public class MultiStoreSettingData
    {
        public int StoreScope { get; set; }
        public HashSet<string> OverriddenKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class MultiStoreSettingHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IViewDataAccessor _viewDataAccessor;
        private readonly SmartDbContext _db;
        private readonly ISettingService _settingService;
        private readonly ILocalizedEntityService _leService;
        private readonly bool _isSingleStoreMode;

        private MultiStoreSettingData _data;

        public MultiStoreSettingHelper(
            IHttpContextAccessor httpContextAccessor,
            IViewDataAccessor viewDataAccessor,
            SmartDbContext db,
            ISettingService settingService,
            ILocalizedEntityService leService,
            IStoreContext storeContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _viewDataAccessor = viewDataAccessor;
            _db = db;
            _settingService = settingService;
            _leService = leService;
            _isSingleStoreMode = storeContext.IsSingleStoreMode();
        }

        public void Contextualize(int storeScope)
        {
            Guard.NotNegative(storeScope);

            if (_data?.StoreScope == storeScope)
            {
                return;
            }

            _data = new MultiStoreSettingData { StoreScope = storeScope };
        }

        private void CheckContextualized()
        {
            if (_data == null)
            {
                throw new InvalidOperationException("Call 'Contextualize(int storeScope)' before calling any store bound method.");
            }
        }

        public ViewDataDictionary ViewData
        {
            get => _viewDataAccessor.ViewData;
        }

        public MultiStoreSettingData Data
        {
            get => _data;
        }

        internal async Task<string> FindOverridenSettingKey(
            string settingName, // PriceSettings
            string fieldPrefix, // CustomProperties[PriceSettings]
            string fieldName, // SomePropName
            bool isRootModel, 
            bool allowEmpty, 
            Func<string, Task<string>> storeAccessor)
        {
            var request = _httpContextAccessor.HttpContext?.Request;

            if (!request.HasFormContentType || request.Form == null)
            {
                // This is a GET operation (no form posted), so check against storage
                var key = settingName + '.' + fieldName;
                var storedValue = await storeAccessor(key);
                var overridden = allowEmpty ? storedValue != null : storedValue.HasValue();
                if (overridden)
                {
                    return isRootModel ? fieldPrefix.Grow(fieldName, ".") : key;
                }
            }
            else
            {
                // A POST operation. Only check form.
                if (IsOverrideChecked(fieldPrefix ?? settingName, fieldName, request.Form, out var key))
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

        public void AddOverrideKey(object settings, string name)
        {
            CheckContextualized();

            var key = (settings?.GetType()?.Name.EmptyNull() + '.' + name).Trim('.');
            _data.OverriddenKeys.Add(key);
        }

        public Task DetectOverrideKeysAsync(
            object settings,
            object model,
            bool isRootModel = true,
            Func<string, string> propertyNameMapper = null)
        {
            return DetectOverrideKeysInternal(settings, model, isRootModel, propertyNameMapper, null);
        }

        private async Task DetectOverrideKeysInternal(
            object settings,
            object model,
            bool isRootModel,
            Func<string, string> propertyNameMapper,
            int? localeIndex)
        {
            if (_isSingleStoreMode)
            {
                // Single store mode -> there are no overrides.
                return;
            }

            CheckContextualized();

            var settingType = settings.GetType();
            var settingName = settingType.Name;
            var fieldPrefix = ViewData?.TemplateInfo?.HtmlFieldPrefix.NullEmpty();
            var modelType = model.GetType();
            var modelProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var localizedModelLocal = model as ILocalizedLocaleModel;

            foreach (var prop in modelProperties)
            {
                string key = null;
                var fieldName = propertyNameMapper?.Invoke(prop.Name) ?? prop.Name;
                var settingProperty = settingType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);

                if (settingProperty == null)
                {
                    // Setting is not configurable or missing or whatever... however we don't need the override info.
                    continue;
                }

                if (localizedModelLocal == null)
                {
                    key = await FindOverridenSettingKey(
                        settingName,
                        fieldPrefix,
                        fieldName,
                        isRootModel: isRootModel,
                        allowEmpty: true, 
                        storeAccessor: x => _settingService.GetSettingByKeyAsync<string>(x, storeId: _data.StoreScope));
                }
                else if (localeIndex.HasValue)
                {
                    var localeKey = $"Locales[{localeIndex.Value}].{fieldName}";
                    key = await FindOverridenSettingKey(
                        settingName,
                        fieldPrefix,
                        localeKey,
                        isRootModel: isRootModel,
                        allowEmpty: true,
                        storeAccessor: x => _leService.GetLocalizedValueAsync(localizedModelLocal.LanguageId, _data.StoreScope, settingName, fieldName));
                }

                if (key != null)
                {
                    _data.OverriddenKeys.Add(key);
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
                            await DetectOverrideKeysInternal(settings, locale, false, propertyNameMapper, i);
                            i++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the form key of the control to the list of override setting keys which are used to determine which settings are overriden on store level.
        /// </summary>
        /// <param fieldName="formKey">The key of the input element that represents the control.</param>
        /// <param fieldName="settingName">Name of the setting (will be concatenated with fieldName of settings seperated by dot e.g. SocalSettings.Facebook)</param>
        /// <param fieldName="settings">Settings instance which contains the particular setting (will be concatenated with fieldName of settings seperated by dot e.g. SocalSettings.Facebook)</param>
        public async Task DetectOverrideKeyAsync(string formKey, string settingName, object settings)
        {
            await DetectOverrideKeyAsync(formKey, settings.GetType().Name + "." + settingName);
        }

        /// <summary>
        /// Adds the form key of the control to the list of override setting keys which are used to determine which settings are overriden on store level.
        /// </summary>
        /// <param fieldName="formKey">The key of the input element that represents the control.</param>
        /// <param fieldName="fullSettingName">Fully qualified fieldName of the setting (e.g. SocalSettings.Facebook)</param>
        public async Task DetectOverrideKeyAsync(string formKey, string fullSettingName)
        {
            if (_isSingleStoreMode)
            {
                // Single store mode -> there are no overrides.
                return;
            }

            CheckContextualized();

            var key = formKey;
            if (await _settingService.GetSettingByKeyAsync<string>(fullSettingName, storeId: _data.StoreScope) == null)
            {
                key = null;
            }

            if (key != null)
            {
                _data.OverriddenKeys.Add(key);
            }
        }

        /// <summary>
        /// Updates settings for a store.
        /// </summary>
        /// <param fieldName="settings">Settings class instance.</param>
        /// <param fieldName="form">Form value collection.</param>
        /// <param fieldName="settingService">Setting service.</param>
        /// <param fieldName="propertyNameMapper">Function to map property names. Return <c>string.Empty</c> to skip a property.</param>
        public async Task UpdateSettingsAsync(
            object settings,
            IFormCollection form,
            Func<string, string> propertyNameMapper = null)
        {
            CheckContextualized();

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

                if (_data.StoreScope == 0 || IsOverrideChecked(settingName, name, form, out _))
                {
                    dynamic value = prop.GetValue(settings);
                    await _settingService.ApplySettingAsync(key, value ?? string.Empty, _data.StoreScope);
                }
                else if (_data.StoreScope > 0)
                {
                    await _settingService.RemoveSettingAsync(key, _data.StoreScope);
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task ApplySettingAsync(
            string formKey,
            string settingName,
            object settings,
            IFormCollection form)
        {
            CheckContextualized();
            
            var settingType = settings.GetType();

            if (_data.StoreScope == 0 || IsOverrideChecked(null, formKey, form, out _))
            {
                var prop = settingType.GetProperty(settingName);
                if (prop != null)
                {
                    dynamic value = prop.GetValue(settings);
                    var key = settingType.Name + "." + settingName;
                    await _settingService.ApplySettingAsync(key, value ?? string.Empty, _data.StoreScope);
                }
            }
            else if (_data.StoreScope > 0)
            {
                var key = settingType.Name + "." + settingName;
                await _settingService.RemoveSettingAsync(key, _data.StoreScope);
            }
        }
    }
}
