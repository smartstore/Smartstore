//using System.Text.RegularExpressions;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.Extensions.DependencyInjection;
//using Smartstore.Core;
//using Smartstore.Events;
//using Smartstore.Web.Rendering;
//using Smartstore.Web.Rendering.Events;

//namespace Smartstore.DevTools
//{
//    /// <summary>
//    /// Renders widget zone preview.
//    /// </summary>
//    public class ViewZoneRenderingEventHandler : IConsumer
//    {
//        public void HandleEvent(ViewZoneRenderingEvent message, 
//            ProfilerSettings profilerSettings)
//        {
//            var zone = message.Zone;

//            if (!profilerSettings.DisplayWidgetZones || zone.PreviewDisabled || zone.ReplaceContent) // TODO: ReplaceContent check
//            {
//                return;
//            }

//            var httpContext = message.ViewContext.HttpContext;
//            if (!httpContext.Request.IsNonAjaxGet())
//            {
//                return;
//            }

//            if (!ShouldRender(httpContext))
//            {
//                return;
//            }

//            // Exclude PageBuilder stories that are being edited.
//            if (message.Model?.GetType()?.Name == "Story")
//            {
//                return;
//            }

//            var tag = new TagBuilder("span");
//            tag.Attributes["class"] = "widget-zone-info badge badge-primary badge-subtle badge-ring text-truncate";
//            tag.Attributes["title"] = zone.Name;
//            tag.InnerHtml.AppendHtml($"<span class=\"text-truncate\">{zone.Name}</span>");

//            if (!string.IsNullOrEmpty(zone.PreviewCssClass))
//            {
//                tag.AppendCssClass(zone.PreviewCssClass);
//            }

//            if (!string.IsNullOrEmpty(zone.PreviewCssStyle))
//            {
//                tag.Attributes["style"] = zone.PreviewCssStyle;
//            }

//            message.Content.PostContent.AppendHtml(tag);
//            message.HasPreview = true;
//        }

//        private static bool ShouldRender(HttpContext httpContext)
//        {
//            var workContext = httpContext.RequestServices.GetRequiredService<IWorkContext>();
            
//            if (!workContext.CurrentCustomer.IsAdmin())
//            {
//                if (httpContext.Request.Path.StartsWithSegments("/pdf", StringComparison.InvariantCultureIgnoreCase))
//                {
//                    return false;
//                }

//                return httpContext.Connection.IsLocal();
//            }

//            return true;
//        }
//    }
//}