using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Smartstore
{
    public static class HttpExtensions
    {
        public static bool IsAdminArea(this HttpRequest request)
        {
            Guard.NotNull(request, nameof(request));

            // TODO: Not really reliable. Change this.

            // Try route
            if (request.HttpContext.TryGetRouteValueAs<string>("area", out var area) && area.EqualsNoCase("admin"))
            {
                // INFO: Module area views can also render in backend. So don't assign false if area is not "admin".
                return true;
            }

            // Try URL prefix
            if (request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
