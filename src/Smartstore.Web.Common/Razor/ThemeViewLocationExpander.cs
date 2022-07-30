using Microsoft.AspNetCore.Mvc.Razor;
using Smartstore.Core.Theming;

namespace Smartstore.Web.Razor
{
    internal class ThemeViewLocationExpander : IViewLocationExpander
    {
        const string ParamKey = "theme";

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.TryGetValue(ParamKey, out var themeName))
            {
                var themeRegistry = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeRegistry>();
                var descriptor = themeRegistry.GetThemeDescriptor(themeName);
                var themeViewLocations = new List<string>(4);

                while (descriptor != null)
                {
                    // INFO: we won't rely on ModularFileProvider's ability to find files in the 
                    // theme hierarchy chain, because of possible view path mismatches. Any mismatch
                    // starts the razor compiler, and we don't want that.
                    themeViewLocations.Add($"{descriptor.Path}Views/{{1}}/{{0}}" + RazorViewEngine.ViewExtension);
                    themeViewLocations.Add($"{descriptor.Path}Views/Shared/{{0}}" + RazorViewEngine.ViewExtension);
                    descriptor = descriptor.BaseTheme;
                }

                return themeViewLocations.Union(viewLocations);
            }

            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var workContext = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IWorkContext>();
            if (workContext.IsAdminArea)
            {
                // TODO: (core) Find a way to make public partials themeable even when they are called from Admin area. Checking specific partial names is only a distress solution.
                if (!context.ViewName.EqualsNoCase("ConfigureTheme"))
                {
                    // Backend is not themeable
                    return;
                }
            }

            var themeContext = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeContext>();
            var currentTheme = themeContext.CurrentTheme;
            if (currentTheme != null)
            {
                context.Values[ParamKey] = currentTheme.Name;
            }
        }
    }
}
