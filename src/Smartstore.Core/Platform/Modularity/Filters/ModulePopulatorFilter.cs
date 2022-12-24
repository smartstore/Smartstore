using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Tries to resolve the originating module and populates
    /// DataTokens with the module name so that any view location
    /// expander can rely on this.
    /// </summary>
    public class ModulePopulatorFilter : IActionFilter
    {
        const string ParamKey = "module";

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
            {
                var moduleCatalog = context.HttpContext.RequestServices.GetRequiredService<IModuleCatalog>();
                var module = moduleCatalog.GetModuleByAssembly(actionDescriptor.ControllerTypeInfo.Assembly);

                if (module != null && module.Theme.IsEmpty())
                {
                    // A theme companion module should not expand, because their views
                    // are located in /Themes/[Theme]/Views via symlink.
                    context.RouteData.DataTokens[ParamKey] = module.Name;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
