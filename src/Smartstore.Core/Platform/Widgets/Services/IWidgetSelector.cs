using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Resolves widgets for zones.
    /// </summary>
    public interface IWidgetSelector
    {
        /// <summary>
        /// Resolves all widgets for the given zone.
        /// </summary>
        /// <param name="zone">Widget zone name.</param>
        /// <param name="viewContext">Current <see cref="ViewContext"/>.</param>
        /// <param name="model">View model</param>
        /// <returns>A list of <see cref="WidgetInvoker"/> instances that should be injected into the zone.</returns>
        Task<IEnumerable<WidgetInvoker>> GetWidgetsAsync(string zone, ViewContext viewContext, object model = null);
    }
}
