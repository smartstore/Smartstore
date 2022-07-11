using Microsoft.AspNetCore.Mvc;
using Smartstore.Web.Components;
using StackExchange.Profiling;

namespace Smartstore.DevTools.Components
{
    public class MiniProfilerViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return HtmlContent(MiniProfiler.Current.RenderIncludes(HttpContext, null));
        }
    }
}