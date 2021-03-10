using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Smartstore.Web.Razor
{
    internal class PartialViewLocationExpander : IViewLocationExpander
    {
        const string ParamKey = "expand-partials";

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            foreach (var format in viewLocations)
            {
                if (context.Values.ContainsKey(ParamKey))
                {
                    yield return format.Replace("{0}", "Layouts/{0}");
                    yield return format.Replace("{0}", "Partials/{0}");
                }
                
                yield return format;
            }
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (!context.IsMainPage && !context.ViewName.StartsWith("Components/", StringComparison.OrdinalIgnoreCase))
            {
                context.Values[ParamKey] = "true";
            }
        }
    }
}
