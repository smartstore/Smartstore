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
                return new[] 
                {
                    $"/Themes/{theme}/Views/{{1}}/{{0}}" + RazorViewEngine.ViewExtension,
                    $"/Themes/{theme}/Views/Shared/{{0}}"  + RazorViewEngine.ViewExtension,
                }
                .Union(viewLocations);
            }
            
            return viewLocations;
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            var workContext = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IWorkContext>();
            if (workContext.IsAdminArea)
            {
                // Backend is not themeable
                return;
            }

            var themeContext = context.ActionContext.HttpContext.RequestServices.GetRequiredService<IThemeContext>();
            var workingTheme = themeContext.WorkingThemeName;
            if (workingTheme.HasValue())
            {
                context.Values["theme"] = themeContext.WorkingThemeName;
            }
        }
    }
}
