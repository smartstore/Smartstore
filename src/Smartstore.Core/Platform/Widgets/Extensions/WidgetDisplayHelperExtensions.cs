using Smartstore.Core.Widgets;

namespace Smartstore
{
    public static class WidgetDisplayHelperExtensions
    {
        /// <summary>
        /// Checks whether the given <paramref name="zone"/> contains at least one widget.
        /// </summary>
        /// <remarks>
        /// Because of deferred result invocation this method cannot check whether 
        /// the widget actually PRODUCES content. E.g., 
        /// if a zone contained a <see cref="ComponentWidgetInvoker"/> with an empty 
        /// result after invocation, this method would still return <c>true</c>.
        /// </remarks>
        /// <param name="zone">The zone name to check.</param>
        public static bool ZoneHasWidgets(this IDisplayHelper displayHelper, string zone)
        {
            return displayHelper.Resolve<IWidgetProvider>().HasWidgets(zone);
        }
    }
}