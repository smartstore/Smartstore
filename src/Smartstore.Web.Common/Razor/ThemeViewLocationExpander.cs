using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Smartstore.Core.Theming;

namespace Smartstore.Web.Razor
{
    internal class ThemeViewLocationExpander : IViewLocationExpander
    {
        internal const string ParamKey = "theme";

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.TryGetValue(ParamKey, out var themeName))
            {
                var themeRegistry = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeRegistry>();
                var theme = themeRegistry.GetThemeDescriptor(themeName);
                var themeViewLocations = new List<string>(4);
                
                while (theme != null)
                {
                    // INFO: we won't rely on ModularFileProvider's ability to find files in the 
                    // theme hierarchy chain, because of possible view path mismatches. Any mismatch
                    // starts the razor compiler, and we don't want that.

                    var ext = RazorViewEngine.ViewExtension;
                    var module = theme.CompanionModule;
                    if (module == null)
                    {
                        themeViewLocations.Add($"{theme.Path}Views/{{1}}/{{0}}" + ext);
                        themeViewLocations.Add($"{theme.Path}Views/Shared/{{0}}" + ext);
                    }
                    else
                    {
                        // Locate in linked module directory, not in theme directory. 
                        themeViewLocations.Add($"{module.Path}Views/{{1}}/{{0}}" + ext);
                        themeViewLocations.Add($"{module.Path}Views/Shared/{{0}}" + ext);
                    }

                    theme = theme.BaseTheme;
                }

                return themeViewLocations.Union(viewLocations);
            }

            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (IsThemeableRequest(context, out var currentTheme))
            {
                context.Values[ParamKey] = currentTheme.Name;
            }
        }

        private static bool IsThemeableRequest(ViewLocationExpanderContext context, out ThemeDescriptor currentTheme)
        {
            var services = context.ActionContext.HttpContext.RequestServices;
            var themeContext = services.GetRequiredService<IThemeContext>();

            currentTheme = themeContext.CurrentTheme;
            if (currentTheme == null)
            {
                return false;
            }

            var workContext = services.GetRequiredService<IWorkContext>();
            if (!workContext.IsAdminArea)
            {
                // Public frontend is always themeable
                return true;
            }

            if (currentTheme.CompanionModule != null)
            {
                // A theme module is themeable even in backend
                if (context.ActionContext.ActionDescriptor is ControllerActionDescriptor cad)
                {
                    if (cad.ControllerTypeInfo.Assembly == currentTheme.CompanionModule.Module.Assembly)
                    {
                        return true;
                    }
                }
            }

            if (context.ViewName.EqualsNoCase("ConfigureTheme"))
            {
                // TODO: (core) Find a way to make public partials themeable even when they are called from Admin area.
                // Checking specific partial names is only a distress solution.
                return true;
            }

            return false;
        }
    }
}
