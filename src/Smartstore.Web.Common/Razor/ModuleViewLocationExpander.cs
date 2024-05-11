using Microsoft.AspNetCore.Mvc.Razor;
using Smartstore.Core.Theming;
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
            _moduleCatalog = Guard.NotNull(moduleCatalog);
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.TryGetValue(ParamKey, out var moduleName))
            {
                var module = _moduleCatalog.GetModuleByName(moduleName); // FAQ

                if (module != null)
                {
                    var ext = RazorViewEngine.ViewExtension;
                    var moduleViewLocations = new List<string>(4);

                    // Check if there are dependencies for this module in the current theme module
                    if (context.Values.TryGetValue(ThemeViewLocationExpander.ParamKey, out var themeName)) // HP
                    {
                        var themeModule = _moduleCatalog.GetModuleByTheme(themeName);
                        if (themeModule != null && themeModule.DependsOn.Contains(module.SystemName)) 
                        {
                            // Current theme's companion module depends on current module
                            var themeRegistry = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeRegistry>();
                            var theme = themeRegistry.GetThemeDescriptor(themeName);

                            moduleViewLocations.Add($"{theme.Path}Views/{{1}}/{{0}}" + ext);
                            moduleViewLocations.Add($"{theme.Path}Views/Shared/{{0}}" + ext);
                        }
                    }

                    moduleViewLocations.Add($"{module.Path}Views/{{1}}/{{0}}" + ext);
                    moduleViewLocations.Add($"{module.Path}Views/Shared/{{0}}" + ext);

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
