using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Configuration;
using Smartstore.Engine;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Modelling
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class LoadSettingAttribute : Attribute, IAsyncActionFilter
    {
        public sealed class SettingParam
        {
            public ISettings Instance { get; set; }
            public ParameterDescriptor Parameter { get; set; }
        }

        protected int _storeId;
        protected SettingParam[] _settingParams;

        public LoadSettingAttribute()
            : this(true)
        {
        }

        public LoadSettingAttribute(bool updateParameterFromStore)
        {
            UpdateParameterFromStore = updateParameterFromStore;
        }

        public bool UpdateParameterFromStore { get; set; }
        public bool IsRootedModel { get; set; }
        public ICommonServices Services { get; set; }

        public virtual async Task OnActionExecutionAsync(ActionExecutingContext filterContext, ActionExecutionDelegate next)
        {
            // Get the current configured store id
            var services = EngineContext.Current.Scope.Resolve<ICommonServices>();
            var controller = filterContext.Controller as Controller;
            _storeId = controller.GetActiveStoreScopeConfiguration(services.StoreContext, services.WorkContext);
            Func<ParameterDescriptor, bool> predicate = (x) => new[] { "storescope", "storeid" }.Contains(x.Name, StringComparer.OrdinalIgnoreCase);
            var storeScopeParam = FindActionParameters<int>(filterContext.ActionDescriptor, false, false, predicate).FirstOrDefault();
            if (storeScopeParam != null)
            {
                // TODO: (mh) (core) Shit!!! Parameters object changed massivly. Probably another ModelBinder is needed for this to work. Research & implement!
                // We found an action param named storeScope with type int. Assign our storeId to it.
                //var parameter = filterContext.ActionDescriptor.Parameters.Where(x => x.Name == storeScopeParam.Name).FirstOrDefault();
                //filterContext.ActionDescriptor.Parameters.Remove(storeScopeParam);
                //filterContext.ActionDescriptor.Parameters.Add(parameter);
            }

            // Find the required ISettings concrete types in ActionDescriptor.GetParameters()
            _settingParams = await FindActionParameters<ISettings>(filterContext.ActionDescriptor)
                .SelectAsync(async x =>
                {
                    // Load settings for the settings type obtained with FindActionParameters<ISettings>()
                    var settings = UpdateParameterFromStore
                        ? await services.SettingFactory.LoadSettingsAsync(x.ParameterType, _storeId)
                        : filterContext.ActionDescriptor.Parameters.Where(y => y.Name == x.Name) as ISettings;

                    if (settings == null)
                    {
                        throw new InvalidOperationException($"Could not load settings for type '{x.ParameterType.FullName}'.");
                    }

                    // Replace settings from action parameters with our loaded settings.
                    if (UpdateParameterFromStore)
                    {
                        // TODO: (mh) (core) Shit!!! Parameters object changed massivly. Probably another ModelBinder is needed for this to work. Research & implement!
                        //filterContext.ActionDescriptor.Parameters[x.Name] = settings;
                        //var parameter = filterContext.ActionDescriptor.Parameters.Where(y => y.Name == x.Name).FirstOrDefault();
                        //filterContext.ActionDescriptor.Parameters.Remove(x);
                        //filterContext.ActionDescriptor.Parameters.Add(parameter);
                    }

                    return new SettingParam
                    {
                        Instance = settings,
                        Parameter = x
                    };
                })
                .ToArrayAsync();

            var executedContext = await next();
            var viewResult = executedContext.Result as ViewResult;

            if (viewResult != null)
            {
                var model = viewResult.Model;

                if (model == null)
                {
                    // Nothing to override. E.g. insufficient permission.
                    return;
                }

                var modelType = model.GetType();
                var settingsHelper = new StoreDependingSettingHelper(controller.ViewData);
                if (IsRootedModel)
                {
                    settingsHelper.CreateViewDataObject(_storeId);
                }

                foreach (var param in _settingParams)
                {
                    var settingInstance = param.Instance;
                    var modelInstance = model;

                    if (IsRootedModel)
                    {
                        modelInstance = modelType.GetProperty(settingInstance.GetType().Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(model);
                        if (modelInstance == null)
                        {
                            continue;
                        }
                    }

                    settingsHelper.GetOverrideKeys(settingInstance, modelInstance, _storeId, services.Settings, !IsRootedModel);
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
    }
}
