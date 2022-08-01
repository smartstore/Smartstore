using System.Reflection;

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;

namespace Smartstore.Web.Modelling.Settings
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class LoadSettingAttribute : TypeFilterAttribute
    {
        public LoadSettingAttribute()
            : this(true)
        {
        }

        public LoadSettingAttribute(bool bindParameterFromStore)
            : this(typeof(LoadSettingFilter), bindParameterFromStore)
        {
        }

        protected LoadSettingAttribute(Type filterType, bool bindParameterFromStore)
            : base(filterType)
        {
            BindParameterFromStore = bindParameterFromStore;
            Arguments = new object[] { this };
        }

        public bool BindParameterFromStore { get; set; }
        public bool IsRootedModel { get; set; }
    }

    internal class LoadSettingFilter : IAsyncActionFilter
    {
        public sealed class SettingParam
        {
            public ISettings Instance { get; set; }
            public ParameterDescriptor Parameter { get; set; }
        }

        private readonly LoadSettingAttribute _attribute;
        protected readonly ICommonServices _services;
        protected readonly MultiStoreSettingHelper _settingHelper;

        protected int _storeId;
        protected SettingParam[] _settingParams;

        public LoadSettingFilter(LoadSettingAttribute attribute, ICommonServices services, MultiStoreSettingHelper settingsHelper)
        {
            _attribute = attribute;
            _services = services;
            _settingHelper = settingsHelper;
        }

        public virtual async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await OnActionExecutingAsync(context);
            await OnActionExecutedAsync(await next());
        }

        protected async Task OnActionExecutingAsync(ActionExecutingContext context)
        {
            // Get the current configured store id
            _storeId = GetActiveStoreScopeConfiguration();
            Func<ParameterDescriptor, bool> predicate = (x) => new[] { "storescope", "storeid" }.Contains(x.Name, StringComparer.OrdinalIgnoreCase);
            var storeScopeParam = FindActionParameters<int>(context.ActionDescriptor, false, false, predicate).FirstOrDefault();
            if (storeScopeParam != null)
            {
                // We found an action param named storeScope with type int. Assign our storeId to it.
                context.ActionArguments[storeScopeParam.Name] = _storeId;
            }

            // Find the required ISettings concrete types in ActionDescriptor.GetParameters()
            _settingParams = await FindActionParameters<ISettings>(context.ActionDescriptor)
                .SelectAwait(async x =>
                {
                    // Load settings for the settings type obtained with FindActionParameters<ISettings>()
                    var settingType = x.ParameterType;
                    var settings = _attribute.BindParameterFromStore
                            ? await _services.SettingFactory.LoadSettingsAsync(settingType, _storeId)
                            : context.ActionArguments[x.Name] as ISettings;

                    if (settings == null)
                    {
                        throw new InvalidOperationException($"Could not load settings for type '{settingType.FullName}'.");
                    }

                    // Replace settings from action parameters with our loaded settings.
                    if (_attribute.BindParameterFromStore)
                    {
                        // In save mode: DON'T pass the setting instance itself to the controller action.
                        // Setting classes are chached as singletons. The action will most likely
                        // update the instance.
                        if (this is SaveSettingFilter)
                        {
                            settings = settings.Clone();
                        }

                        context.ActionArguments[x.Name] = settings;
                    }

                    return new SettingParam
                    {
                        Instance = settings,
                        Parameter = x
                    };
                })
                .ToArrayAsync();

        }

        protected async Task OnActionExecutedAsync(ActionExecutedContext context)
        {
            if (context.Result is ViewResult viewResult)
            {
                var model = viewResult.Model;

                if (model == null)
                {
                    // Nothing to override. E.g. insufficient permission.
                    return;
                }

                var modelType = model.GetType();
                if (_attribute.IsRootedModel)
                {
                    _settingHelper.CreateViewDataObject(_storeId);
                }

                foreach (var param in _settingParams)
                {
                    var settingInstance = param.Instance;
                    var modelInstance = model;

                    if (_attribute.IsRootedModel)
                    {
                        modelInstance = modelType.GetProperty(settingInstance.GetType().Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(model);
                        if (modelInstance == null)
                        {
                            continue;
                        }
                    }

                    await _settingHelper.DetectOverrideKeysAsync(settingInstance, modelInstance, _storeId, !_attribute.IsRootedModel);
                }
            }
        }

        protected IEnumerable<ParameterDescriptor> FindActionParameters<T>(
            ActionDescriptor actionDescriptor,
            bool requireDefaultConstructor = true,
            bool throwIfNotFound = true,
            Func<ParameterDescriptor, bool> predicate = null)
        {
            Guard.NotNull(actionDescriptor, nameof(actionDescriptor));

            var t = typeof(T);

            var query = actionDescriptor
                .Parameters
                .Where(x => t.IsAssignableFrom(x.ParameterType));

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (throwIfNotFound && !query.Any())
            {
                throw new InvalidOperationException(
                    $"A controller action method with a '{GetType().Name}' attribute requires an action parameter of type '{t.Name}' in order to execute properly.");
            }

            if (requireDefaultConstructor)
            {
                foreach (var param in query)
                {
                    if (!param.ParameterType.HasDefaultConstructor())
                    {
                        throw new InvalidOperationException($"The parameter '{param.Name}' must have a default parameterless constructor.");
                    }
                }
            }

            return query;
        }

        protected int GetActiveStoreScopeConfiguration()
        {
            var storeId = _services.WorkContext.CurrentCustomer.GenericAttributes.AdminAreaStoreScopeConfiguration;
            var store = _services.StoreContext.GetStoreById(storeId);
            return store != null ? store.Id : 0;
        }
    }
}
