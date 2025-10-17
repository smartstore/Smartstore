#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Events;

namespace Smartstore.Web.Rendering.Events
{
    public class ViewZoneRenderingEvent : IEventMessage
    {
        public ViewZoneRenderingEvent(IWidgetZone zone, ZoneHtmlContent content, ViewContext viewContext)
        {
            Zone = Guard.NotNull(zone);
            Content = Guard.NotNull(content);
            ViewContext = Guard.NotNull(viewContext);
        }

        /// <summary>
        /// The widget zone.
        /// </summary>
        public IWidgetZone Zone { get; }

        /// <summary>
        /// The zone content.
        /// </summary>
        public ZoneHtmlContent Content { get; }

        /// <summary>
        /// The view context.
        /// </summary>
        public ViewContext ViewContext { get; }

        /// <summary>
        /// The view model.
        /// </summary>
        public object? Model { get; init; }
    }
}
