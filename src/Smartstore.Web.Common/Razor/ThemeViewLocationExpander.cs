using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Razor
{
    internal class ThemeViewLocationExpander : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context.Values.TryGetValue("theme", out var theme))
            {
                var themeRegistry = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeRegistry>();
                var descriptor = themeRegistry.GetThemeDescriptor(theme);
                var themeLocations = new List<string>(4);

                while (descriptor != null)
                {
                    themeLocations.Add($"/Themes/{descriptor.ThemeName}/Views/{{1}}/{{0}}" + RazorViewEngine.ViewExtension);
                    themeLocations.Add($"/Themes/{descriptor.ThemeName}/Views/Shared/{{0}}" + RazorViewEngine.ViewExtension);
                    descriptor = descriptor.BaseTheme;
                }

                return themeLocations.Union(viewLocations);
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
                context.Values["theme"] = currentTheme.ThemeName;
            }
        }
    }
}
