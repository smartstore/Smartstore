using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Resolves widgets for zones.
    /// </summary>
    public interface IWidgetSelector
    {
        /// <summary>
        /// Checks whether the given <paramref name="zone"/> contains at least one widget
        /// that produces non-whitespace content.
        /// </summary>
        /// <remarks>
        /// This method must actually INVOKE widgets in order to scan for content.
        /// It will break iteration on first found real content though.
        /// But to check for the mere existence of widgets in a zone it is better to call 
        /// <see cref="GetWidgetsAsync(string, object)"/>.Any() instead.
        /// </remarks>
        /// <param name="zone">The zone name to check.</param>
        /// <param name="viewContext">The current view context.</param>
        Task<bool> HasContentAsync(string zone, ViewContext viewContext);

        /// <summary>
        /// Resolves all widgets for the given zone.
        /// </summary>
        /// <param name="zone">Widget zone name.</param>
        /// <param name="model">Optional view model</param>
        /// <returns>A list of <see cref="WidgetInvoker"/> instances that should be injected into the zone.</returns>
        Task<IEnumerable<WidgetInvoker>> GetWidgetsAsync(string zone, object model = null);
    }
}
