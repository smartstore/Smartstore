using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Modelling.Settings
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class SaveSettingAttribute : LoadSettingAttribute
    {
        public SaveSettingAttribute()
            : base(typeof(SaveSettingFilter), true)
        {
        }

        public SaveSettingAttribute(bool bindParameterFromStore)
            : base(typeof(SaveSettingFilter), bindParameterFromStore)
        {
            Arguments = new object[] { this };
        }
    }

    internal class SaveSettingFilter : LoadSettingFilter
    {
        private IFormCollection _form;

        public SaveSettingFilter(SaveSettingAttribute attribute, ICommonServices services, MultiStoreSettingHelper settingHelper)
            : base(attribute, services, settingHelper)
        {
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await OnActionExecutingAsync(context);

            if (context.ModelState.IsValid)
            {
                // Find the required FormCollection parameter in ActionDescriptor.GetParameters()
                var formParam = FindActionParameters<IFormCollection>(context.ActionDescriptor, requireDefaultConstructor: false, throwIfNotFound: false).FirstOrDefault();
                _form = formParam != null
                    ? (IFormCollection)context.ActionArguments[formParam.Name]
                    : await context.HttpContext.Request.ReadFormAsync();
            }

            var executedContext = await next();

            if (executedContext.ModelState.IsValid)
            {
                var updateSettings = true;

                if (context.Result is RedirectToRouteResult redirectResult)
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
                        await _settingHelper.UpdateSettingsAsync(param.Instance, _form, _storeId);
                    }
                }
            }

            await OnActionExecutedAsync(executedContext);
        }
    }
}