using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Widgets;

namespace Smartstore
{
    public static class WidgetDisplayHelperExtensions
    {
        /// <summary>
        /// Checks whether the given <paramref name="zoneName"/> contains at least one widget.
        /// </summary>
        /// <remarks>
        /// Because of deferred result invocation this method cannot check whether 
        /// the widget actually PRODUCES content. E.g., 
        /// if a zone contained a <see cref="ComponentWidget"/> with an empty 
        /// result after invocation, this method would still return <c>true</c>.
        /// To check whether a zone actually contains non-whitespace content, 
        /// call <see cref="ZoneHasContentAsync(IDisplayHelper, string, ViewContext)"/> instead.
        /// </remarks>
        /// <param name="zoneName">The zone name to check.</param>
        public static ValueTask<bool> ZoneHasWidgetsAsync(this IDisplayHelper displayHelper, string zoneName)
        {
            return displayHelper.Resolve<IWidgetSelector>().EnumerateWidgetsAsync(zoneName).AnyAsync();
        }

        /// <summary>
        /// Checks whether the given <paramref name="zoneName"/> contains at least one widget
        /// that produces non-whitespace content.
        /// </summary>
        /// <remarks>
        /// This method must actually INVOKE widgets in order to scan for content.
        /// It will break iteration on first found real content though.
        /// But to check for the mere existence of widgets in a zone it is better to call 
        /// <see cref="IWidgetSelector.EnumerateWidgetsAsync(IWidgetZone, object)"/>.AnyAsync() instead.
        /// </remarks>
        /// <param name="zoneName">The zone name to check.</param>
        /// <param name="viewContext">The current view context.</param>
        public static Task<bool> ZoneHasContentAsync(this IDisplayHelper displayHelper, string zoneName, ViewContext viewContext)
        {
            return displayHelper.Resolve<IWidgetSelector>().HasContentAsync(zoneName, viewContext);
        }
    }
}