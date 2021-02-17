using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Core.Content.Menus
{
    public static partial class MenuUrlHelperExtensions
    {
        public static bool IsCurrentMenuItem(this IUrlHelper urlHelper, object navigatable)
        {
            Guard.NotNull(navigatable, nameof(navigatable));

            var routeData = urlHelper.ActionContext.HttpContext.GetRouteData();
            if (routeData != null)
            {
                // TODO: (mh) (core) Move INavigatable & stuff to core first. Then we can implement.
            }

            return false;
        }

        public static string MenuItem(this IUrlHelper urlHelper, object navigatable)
        {
            Guard.NotNull(navigatable, nameof(navigatable));

            // TODO: (mh) (core) Move INavigatable & stuff to core first. Then we can implement.
            return null;
        }
    }
}
