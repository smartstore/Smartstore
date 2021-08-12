using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
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
                var themeLocations = new List<string>
                {
                    // ModularFileProvider will lookup in base themes.
                    $"{descriptor.Path}Views/{{1}}/{{0}}" + RazorViewEngine.ViewExtension,
                    $"{descriptor.Path}Views/Shared/{{0}}" + RazorViewEngine.ViewExtension
                };

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
                context.Values[ParamKey] = currentTheme.Name;
            }
        }
    }
}
