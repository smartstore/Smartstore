using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;

namespace Smartstore.Web.Modelling.Settings
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class SaveSettingAttribute : LoadSettingAttribute
    {
        public SaveSettingAttribute()
            : base(typeof(SaveSettingFilter), true)
        {
        }

        public SaveSettingAttribute(bool updateParameterFromStore)
            : base(typeof(SaveSettingFilter), updateParameterFromStore)
        {
            Arguments = new object[] { this };
        }
    }

    internal class SaveSettingFilter : LoadSettingFilter
    {
        private IFormCollection _form;
        private readonly StoreDependingSettingHelper _storeDependingSettings;

        public SaveSettingFilter(SaveSettingAttribute attribute, ICommonServices services, StoreDependingSettingHelper storeDependingSettings)
            : base (attribute, services, storeDependingSettings)
        {
            _storeDependingSettings = storeDependingSettings;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await OnActionExecutingAsync(context);

            if (!context.ModelState.IsValid)
            {
                return;
            }

            // Find the required FormCollection parameter in ActionDescriptor.GetParameters()
            var formParam = FindActionParameters<IFormCollection>(context.ActionDescriptor, requireDefaultConstructor: false, throwIfNotFound: false).FirstOrDefault();
            _form = formParam != null
                ? (IFormCollection)context.ActionArguments[formParam.Name]
                : await context.HttpContext.Request.ReadFormAsync();

            var executedContext = await next();
            var viewResult = executedContext.Result as ViewResult;

            if (executedContext.ModelState.IsValid)
            {
                var updateSettings = true;
                var redirectResult = context.Result as RedirectToRouteResult;
                if (redirectResult != null)
                {
                    var controllerName = redirectResult.RouteValues.GetControllerName();
                    var areaName = redirectResult.RouteValues.GetAreaName();
                    if (controllerName.EqualsNoCase("security") && areaName.EqualsNoCase("admin"))
                    {
                        // Insufficient permission. We must not save because the action did not run.
                        updateSettings = false;
                    }
                }

                if (updateSettings)
                {
                    foreach (var param in _settingParams)
                    {
                        await _storeDependingSettings.UpdateSettingsAsync(param.Instance, _form, _storeId);
                    }
                }
            }

            await OnActionExecutedAsync(executedContext);
        }
    }
}