namespace Smartstore.Web.Rendering.Events
{
    public class ViewZoneRenderingEvent(string zoneName, object model, ZoneHtmlContent content)
    {
        /// <summary>
        /// The name of the zone which is rendered.
        /// </summary>
        public string ZoneName { get; } = zoneName;

        /// <summary>
        /// The view model.
        /// </summary>
        public object Model { get; } = model;

        /// <summary>
        /// The zone content.
        /// </summary>
        public ZoneHtmlContent Content { get; } = content;
    }
}
