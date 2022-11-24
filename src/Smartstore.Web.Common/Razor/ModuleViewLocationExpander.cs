using Microsoft.AspNetCore.Mvc.Razor;
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

        private readonly IModuleCatalog _moduleCatalog;

        public ModuleViewLocationExpander(IModuleCatalog moduleCatalog)
        {
            _moduleCatalog = Guard.NotNull(moduleCatalog, nameof(moduleCatalog));
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.TryGetValue(ParamKey, out var moduleName))
            {
                var module = _moduleCatalog.GetModuleByName(moduleName);

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
            if (context.ActionContext.RouteData.DataTokens.TryGetValueAs<string>(ParamKey, out var moduleName))
            {
                context.Values[ParamKey] = moduleName;
            }
        }
    }
}
