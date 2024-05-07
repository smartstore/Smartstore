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
                var module = _moduleCatalog.GetModuleByName(moduleName);

                if (module != null)
                {
                    var ext = RazorViewEngine.ViewExtension;

                    // Resolve current theme
                    if (context.Values.TryGetValue(ThemeViewLocationExpander.ParamKey, out var themeName))
                    {
                        // Try to get the companion module of current theme
                        var themeModule = _moduleCatalog.GetModuleByTheme(themeName);
                        if (themeModule != null && themeModule.DependsOn.Contains(module.SystemName))
                        {
                            // Current theme's companion module depends on current module
                            var themeRegistry = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeRegistry>();
                            var theme = themeRegistry.GetThemeDescriptor(themeName);

                            var combinedViewLocations = new[]
                            {
                                $"{theme.Path}Views/{{1}}/{{0}}" + ext,
                                $"{theme.Path}Views/Shared/{{0}}" + ext,
                                $"{module.Path}Views/{{1}}/{{0}}" + ext,
                                $"{module.Path}Views/Shared/{{0}}" + ext,
                            };

                            return combinedViewLocations.Union(viewLocations);
                        }
                    }
                    
                    var moduleViewLocations = new[]
                    {
                        $"{module.Path}Views/{{1}}/{{0}}" + ext,
                        $"{module.Path}Views/Shared/{{0}}" + ext,
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
