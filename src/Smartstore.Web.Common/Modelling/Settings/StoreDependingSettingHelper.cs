using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;

namespace Smartstore.Web.Modelling.Settings
{
    public class StoreDependingSettingHelper
    {
        private readonly IViewDataAccessor _viewDataAccessor;
        private readonly SmartDbContext _db;
        private readonly ISettingService _settingService;
        private readonly ILocalizedEntityService _leService;

        public StoreDependingSettingHelper(
            IViewDataAccessor viewDataAccessor,
            SmartDbContext db,
            ISettingService settingService, 
            ILocalizedEntityService leService)
        {
            _viewDataAccessor = viewDataAccessor;
            _db = db;
            _settingService = settingService;
            _leService = leService;
        }

        public static string ViewDataKey => "StoreDependingSettingData";

        public ViewDataDictionary ViewData
        {
            get => _viewDataAccessor.ViewData;
        }

        public StoreDependingSettingData Data
        {
            get => ViewData[ViewDataKey] as StoreDependingSettingData;
        }

        public bool IsOverrideChecked(object settings, string name, IFormCollection form)
        {
            var key = settings.GetType().Name + "." + name;
            return IsOverrideChecked(key, form);
        }

        private bool IsOverrideChecked(string settingKey, IFormCollection form)
        {
            var rawOverrideKey = settingKey + "_OverrideForStore";
            if (form.ContainsKey(rawOverrideKey))
            {
                var checkboxValue = form[rawOverrideKey].FirstOrDefault().EmptyNull().ToLower();
                return checkboxValue.Contains("on") || checkboxValue.Contains("true");
            }

            return false;
        }

        public void AddOverrideKey(object settings, string name)
        {
            if (Data == null)
            {
                throw new SmartException("You must call GetOverrideKeys or CreateViewDataObject before AddOverrideKey.");
            }

            var key = settings.GetType().Name + "." + name;
            Data.OverrideSettingKeys.Add(key);
        }

        public void CreateViewDataObject(int activeStoreScopeConfiguration, string rootSettingClass = null)
        {
            ViewData[ViewDataKey] = new StoreDependingSettingData
            {
                ActiveStoreScopeConfiguration = activeStoreScopeConfiguration,
                RootSettingClass = rootSettingClass
            };
        }

        public Task GetOverrideKeysAsync(
            object settings,
            object model,
            int storeId,
            bool isRootModel = true,
            Func<string, string> propertyNameMapper = null)
        {
            return GetOverrideKeysInternal(settings, model, storeId, isRootModel, propertyNameMapper, null);
        }

        private async Task GetOverrideKeysInternal(
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

            var data = Data ?? new StoreDependingSettingData();
            var settingType = settings.GetType();
            var modelType = model.GetType();
            var settingName = settingType.Name;
            var modelProperties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var localizedModelLocal = model as ILocalizedLocaleModel;

            foreach (var prop in modelProperties)
            {
                var name = propertyNameMapper?.Invoke(prop.Name) ?? prop.Name;

                var settingProperty = settingType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

                if (settingProperty == null)
                {
                    // Setting is not configurable or missing or whatever... however we don't need the override info.
                    continue;
                }

                string key = null;

                if (localizedModelLocal == null)
                {
                    key = settingName + "." + name;
                    if (await _settingService.GetSettingByKeyAsync<string>(key, storeId: storeId) == null)
                    {
                        key = null;
                    }
                }
                else if (localeIndex.HasValue)
                {
                    var value = await _leService.GetLocalizedValueAsync(localizedModelLocal.LanguageId, storeId, settingName, name);
                    if (!string.IsNullOrEmpty(value))
                    {
                        key = settingName + "." + "Locales[" + localeIndex.ToString() + "]." + name;
                    }
                }

                if (key != null)
                {
                    data.OverrideSettingKeys.Add(key);
                }
            }

            if (isRootModel)
            {
                data.ActiveStoreScopeConfiguration = storeId;
                data.RootSettingClass = settingName;

                ViewData[ViewDataKey] = data;
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
                            await GetOverrideKeysInternal(settings, locale, storeId, false, propertyNameMapper, i);
                            i++;
                        }
                    }
                }
            }
        }

        public async Task GetOverrideKeyAsync(string formKey, string settingName, object settings, int storeId)
        {
            if (storeId <= 0)
            {
                // Single store mode -> there are no overrides.
                return;
            }

            var key = formKey;
            if (await _settingService.GetSettingByKeyAsync<string>(string.Concat(settings.GetType().Name, ".", settingName), storeId: storeId) == null)
            {
                key = null;
            }

            if (key != null)
            {
                var data = Data ?? new StoreDependingSettingData();
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

                if (!name.HasValue())
                {
                    continue;
                }

                var key = string.Concat(settingName, ".", name);

                if (storeId == 0 || IsOverrideChecked(key, form))
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

        public async Task UpdateSettingAsync(
            string formKey,
            string settingName,
            object settings,
            IFormCollection form,
            int storeId)
        {
            var settingType = settings.GetType();

            if (storeId == 0 || IsOverrideChecked(formKey, form))
            {
                var prop = FastProperty.GetProperty(settingType, settingName);
                if (prop != null)
                {
                    dynamic value = prop.GetValue(settings);
                    var key = string.Concat(settingType.Name, ".", settingName);
                    await _settingService.ApplySettingAsync(key, value ?? string.Empty, storeId);
                }
            }
            else if (storeId > 0)
            {
                var key = string.Concat(settingType.Name, ".", settingName);
                await _settingService.RemoveSettingAsync(key, storeId);
            }

            await _db.SaveChangesAsync();
        }
    }
}
