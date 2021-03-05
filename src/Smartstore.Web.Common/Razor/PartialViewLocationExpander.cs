using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Smartstore.Web.Razor
{
    internal class PartialViewLocationExpander : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            foreach (var format in viewLocations)
            {
                if (!context.IsMainPage)
                {
                    yield return format.Replace("{0}", "Layouts/{0}");
                    yield return format.Replace("{0}", "Partials/{0}");
                }
                
                yield return format;
            }
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }
    }
}
