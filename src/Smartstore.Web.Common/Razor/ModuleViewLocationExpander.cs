using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Razor
{
    /// <summary>
    /// If controller is defined in a module assembly: 
    /// Adds "/Modules/[ModuleName]/Views/{1}/{0}.cshtml" and "/Modules/[ModuleName]/Views/Shared/{0}.cshtml" as view locations.
    /// </summary>
    internal class ModuleViewLocationExpander : IViewLocationExpander
    {
        const string ParamKey = "module";
        
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.TryGetValue(ParamKey, out var moduleName))
            {
                var moduleCatalog = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IModuleCatalog>();
                var module = moduleCatalog.GetModuleByName(moduleName);

                if (module != null)
                {
                    var moduleViewLocations = new[]
                    {
                        $"{module.Path}Views/{{1}}/{{0}}" + RazorViewEngine.ViewExtension,
                        $"{module.Path}Views/Shared/{{0}}" + RazorViewEngine.ViewExtension,
                    };
                    
                    return moduleViewLocations.Union(viewLocations);
                }
            }

            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (context.ActionContext.RouteData.DataTokens.TryGetValue("module", out var value) && value is string str)
            {
                context.Values[ParamKey] = str;
            }
            else if (context.ActionContext.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
            {
                var moduleCatalog = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IModuleCatalog>();
                var module = moduleCatalog.GetModuleByAssembly(actionDescriptor.ControllerTypeInfo.Assembly);

                if (module != null)
                {
                    context.Values[ParamKey] = module.Name;
                }
            }
        }
    }
}
