using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Smartstore.Web.Components;
using StackExchange.Profiling;

namespace Smartstore.DevTools.Components
{
    public class MiniProfilerViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var routeData = HttpContext.GetRouteData();
            var routeId = routeData.Values.GenerateRouteIdentifier();

            if (routeId == "Story.Story")
            {
                return Empty();
            }

            return HtmlContent(MiniProfiler.Current.RenderIncludes(HttpContext, null));
        }
    }
}