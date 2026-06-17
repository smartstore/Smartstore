#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;

namespace Smartstore.Web.Modelling.Settings;

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
    private readonly ISettingFactory _settingFactory;
    private readonly ISettingService _settingService;
    private readonly ILocalizedEntityService _leService;
    private readonly bool _isSingleStoreMode;
    private readonly int _defaultStoreScope;

    private MultiStoreSettingData? _data;

    public MultiStoreSettingHelper(
        IHttpContextAccessor httpContextAccessor,
        IViewDataAccessor viewDataAccessor,
        SmartDbContext db,
        ISettingFactory settingFactory,
        ISettingService settingService,
        ILocalizedEntityService leService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _viewDataAccessor = viewDataAccessor;
        _db = db;
        _settingFactory = settingFactory;
        _settingService = settingService;
        _leService = leService;
        _isSingleStoreMode = storeContext.IsSingleStoreMode();

        if (!_isSingleStoreMode)
        {
            var storeId = workContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
            _defaultStoreScope = storeContext.GetStoreById(storeId)?.Id ?? 0;
        }
    }

    /// <summary>
    /// Configures the current instance to use the specified store scope.
    /// </summary>
    /// <remarks>If the current store scope is already set to the specified value, this method
    /// performs no action.</remarks>
    /// <param name="storeScope">The store scope identifier to apply. Must be zero or a positive integer.</param>
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

    private int EnsureContextualized()
    {
        if (_data == null)
        {
            Contextualize(_defaultStoreScope);
        }

        return _data!.StoreScope;
    }

    public ViewDataDictionary? ViewData
    {
        get => _viewDataAccessor.ViewData;
    }

    public MultiStoreSettingData? Data
    {
        get => _data;
    }

    internal async Task<string?> FindOverridenSettingKey(
        string settingName, // PriceSettings
        string? fieldPrefix, // CustomProperties[PriceSettings]
        string fieldName, // SomePropName
        bool isRootModel,
        bool allowEmpty,
        Func<string, Task<string>> storeAccessor)
    {
        var request = _httpContextAccessor.HttpContext?.Request;

        if (request == null || !request.HasFormContentType || request.Form == null)
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

    /// <summary>
    /// Determines whether the override checkbox for a specified setting property is checked in the provided form
    /// collection.
    /// </summary>
    /// <typeparam name="TSetting">The type of the settings object. Must implement the ISettings interface.</typeparam>
    /// <param name="settingInstance">The instance of the settings object containing the property to check.</param>
    /// <param name="name">The name of the setting property for which to check the override status.</param>
    /// <param name="form">The form collection containing the submitted values.</param>
    /// <returns>true if the override checkbox for the specified setting property is checked; otherwise, false.</returns>
    public static bool IsOverrideChecked<TSetting>(TSetting settingInstance, string name, IFormCollection form)
        where TSetting : ISettings
        => IsOverrideChecked(Guard.NotNull(settingInstance).GetType(), name, form);

    /// <summary>
    /// Determines whether the override checkbox for a given form field is checked, using the specified prefix and
    /// field name.
    /// </summary>
    /// <remarks>The method checks for an override using both the prefixed and unprefixed field names.
    /// The key is constructed by appending '_OverrideForStore' to the field name. The checkbox is considered
    /// checked if the corresponding form value contains 'on' or 'true' (case-insensitive).</remarks>
    /// <param name="name">The name of the form field to check for an override.</param>
    /// <param name="form">The form collection containing the submitted values.</param>
    /// <returns>true if the override checkbox is checked for the specified field; otherwise, false.</returns>
    public static bool IsOverrideChecked(Type settingType, string name, IFormCollection form)
    {
        Guard.NotNull(settingType);
        Guard.NotEmpty(name);
        Guard.NotNull(form);

        return IsOverrideChecked(settingType.Name, name, form, out _);
    }

    internal static bool IsOverrideChecked(string? prefix, string name, IFormCollection form, [MaybeNullWhen(false)] out string? key)
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

    /// <summary>
    /// Adds an override key for the specified settings object and property name to the collection of overridden
    /// keys.
    /// </summary>
    /// <remarks>The override key is constructed using the type name of the settings object and the
    /// specified property name. This method is typically used to track which settings properties have been
    /// explicitly overridden.</remarks>
    /// <param name="settings">The settings object whose property is being overridden. Can be null.</param>
    /// <param name="name">The name of the property to mark as overridden. Cannot be null or empty.</param>
    public void AddOverrideKey(object? settings, string name)
    {
        CheckContextualized();

        var key = (settings?.GetType()?.Name.EmptyNull() + '.' + name).Trim('.');
        _data!.OverriddenKeys.Add(key);
    }

    /// <summary>
    /// Detects and records the keys of settings properties that are overridden for a specific store or locale
    /// within the provided model hierarchy.
    /// </summary>
    /// <remarks>This method traverses the model's properties and, for each property that corresponds
    /// to a configurable setting, determines if an override exists for the current store or locale. If the model
    /// implements ILocalizedModel, the method recursively processes all localized locale models. In single store
    /// mode, override detection is skipped.</remarks>
    /// <param name="settings">The settings object containing the configuration properties to check for overrides.</param>
    /// <param name="model">The model instance whose properties are examined for override detection. May represent a root or localized
    /// model.</param>
    /// <param name="isRootModel">true if the model is the root model in the hierarchy; otherwise, false.</param>
    /// <param name="propertyNameMapper">A function that maps property names to their corresponding field names in the settings object. Used to
    /// resolve property name differences between the model and settings.</param>
    public Task DetectOverrideKeysAsync(
        object settings,
        object model,
        bool isRootModel = true,
        Func<string, string>? propertyNameMapper = null)
    {
        return DetectOverrideKeysInternal(settings, model, isRootModel, propertyNameMapper, null);
    }

    private async Task DetectOverrideKeysInternal(
        object settings,
        object model,
        bool isRootModel,
        Func<string, string>? propertyNameMapper,
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
            string? key = null;
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
                    storeAccessor: x => _settingService.GetSettingByKeyAsync<string>(x, storeId: _data!.StoreScope));
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
                    storeAccessor: x => _leService.GetLocalizedValueAsync(localizedModelLocal.LanguageId, _data!.StoreScope, settingName, fieldName));
            }

            if (key != null)
            {
                _data!.OverriddenKeys.Add(key);
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
        if (await _settingService.GetSettingByKeyAsync<string>(fullSettingName, storeId: _data!.StoreScope) == null)
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
        Func<string, string>? propertyNameMapper = null)
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

            if (_data!.StoreScope == 0 || IsOverrideChecked(settingName, name, form, out _))
            {
                await ApplySettingAsync(key, prop, _data.StoreScope);
            }
            else if (_data.StoreScope > 0)
            {
                await _settingService.RemoveSettingAsync(key, _data.StoreScope);

                if (prop.Property.GetCustomAttribute<GlobalSettingAttribute>() != null)
                {
                    // In case of global setting, apply the default value for the store which is the same as global value.
                    await ApplySettingAsync(key, prop, 0);
                }
            }
        }

        await _db.SaveChangesAsync();

        Task ApplySettingAsync(string key, FastProperty prop, int storeScope)
        {
            dynamic value = prop.GetValue(settings)!;
            return _settingService.ApplySettingAsync(key, value ?? string.Empty, storeScope);
        }
    }

    /// <summary>
    /// Applies a setting value based on the specified form input and store scope asynchronously.
    /// </summary>
    /// <remarks>If the store scope is zero or the override is checked in the form, the method applies
    /// the setting value. Otherwise, it removes the setting for the specified store scope. This method is typically
    /// used in configuration scenarios where settings can be overridden per store.</remarks>
    /// <param name="formKey">The key of the form field used to determine whether the setting should be overridden.</param>
    /// <param name="settingNameOrProp">The name of the setting property or the PropertyInfo object to apply.</param>
    /// <param name="settings">An object containing the settings from which the property value is retrieved.</param>
    /// <param name="form">The form collection containing submitted values and override indicators.</param>
    public async Task ApplySettingAsync(
        string formKey,
        object settingNameOrProp,
        object settings,
        IFormCollection form)
    {
        CheckContextualized();

        var settingName = (settingNameOrProp as string ?? (settingNameOrProp as PropertyInfo)?.Name)!;
        var prop = settingNameOrProp as PropertyInfo;
        var settingType = settings.GetType();

        if (_data!.StoreScope == 0 || IsOverrideChecked(null, formKey, form, out _))
        {
            prop ??= settingType.GetProperty(settingName);
            if (prop != null)
            {
                dynamic value = prop.GetValue(settings)!;
                var key = settingType.Name + "." + settingName;
                await _settingService.ApplySettingAsync(key, value ?? string.Empty, _data!.StoreScope);
            }
        }
        else if (_data.StoreScope > 0)
        {
            var key = settingType.Name + "." + settingName;
            await _settingService.RemoveSettingAsync(key, _data.StoreScope);
        }
    }

    /// <inheritdoc cref="MapModelAsync{TModel, TSetting}(TModel, TSetting, IFormCollection, string, Action{TSetting})" />
    public async Task<TSetting> MapModelAsync<TModel, TSetting>(
        TModel model,
        IFormCollection form,
        string prefix,
        Action<TSetting>? onBeforeMap = null)
        where TSetting : class, ISettings, new()
        where TModel : ModelBase
    {
        var storeScope = EnsureContextualized();
        var settings = await _settingFactory.LoadSettingsAsync<TSetting>(storeScope);

        return await MapModelAsync(model, settings, form, prefix, onBeforeMap);
    }

    /// <summary>
    /// Maps values from the specified model to the provided settings instance, applies additional form values, and
    /// persists changes asynchronously.
    /// </summary>
    /// <remarks>The method clones the provided settings instance before mapping and applies form
    /// values using the specified prefix. Changes are saved to the database asynchronously.</remarks>
    /// <typeparam name="TModel">The type of the source model. Must inherit from ModelBase.</typeparam>
    /// <typeparam name="TSetting">The type of the settings object. Must implement ISettings and have a parameterless constructor.</typeparam>
    /// <param name="model">The source model containing values to map to the settings.</param>
    /// <param name="settings">The settings instance to update with mapped values. A clone of this instance is used for mapping.</param>
    /// <param name="form">The form collection containing additional values to apply to the settings.</param>
    /// <param name="prefix">The prefix used to identify relevant form fields for the settings.</param>
    /// <param name="onBeforeMap">An optional action to perform on the settings instance before mapping occurs.</param>
    /// <returns>A settings instance of type TSetting with values mapped from the model and form, after changes have been persisted.</returns>
    public async Task<TSetting> MapModelAsync<TModel, TSetting>(
        TModel model,
        TSetting settings,
        IFormCollection form,
        string prefix,
        Action<TSetting>? onBeforeMap = null)
        where TSetting : class, ISettings, new()
        where TModel : ModelBase
    {
        settings = (TSetting)settings.Clone();
        onBeforeMap?.Invoke(settings);

        var mapper = MapperFactory.GetMapper<TModel, TSetting>();
        await mapper.MapAsync(model, settings);

        var settingProps = FastProperty.GetProperties(typeof(TSetting)).Values;

        foreach (var prop in settingProps)
        {
            await ApplySettingAsync(
                $"{prefix}.{prop.Name}",
                prop.Property,
                settings,
                form);
        }

        await _db.SaveChangesAsync();

        return settings;
    }

    /// <summary>
    /// Loads settings of the specified type, maps them to the specified model, and returns both the populated model and the settings instance.
    /// </summary>
    /// <typeparam name="TSetting">The type of settings to load. Must implement <see cref="ISettings"/> and have a parameterless constructor.</typeparam>
    /// <typeparam name="TModel">The type of model to map the settings to. Must derive from <see cref="ModelBase"/>.</typeparam>
    /// <param name="prefix">An optional prefix to apply to the HTML field names. Also required to create proper override key names.</param>
    /// <returns>A tuple containing the mapped model and the loaded settings instance.</returns>
    public async Task<(TModel Model, TSetting Settings)> MapSettingsAsync<TSetting, TModel>(string? prefix = null)
        where TSetting : class, ISettings, new()
        where TModel : ModelBase, new()
    {
        if (prefix.HasValue())
        {
            ViewData?.TemplateInfo?.HtmlFieldPrefix = prefix;
        }

        var storeScope = EnsureContextualized();
        var settings = await _settingFactory.LoadSettingsAsync<TSetting>(storeScope);
        var model = await MapperFactory.MapAsync<TSetting, TModel>(settings);

        await DetectOverrideKeysAsync(settings, model);

        return (model, settings);
    }
}