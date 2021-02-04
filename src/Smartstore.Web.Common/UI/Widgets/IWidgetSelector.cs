using System;
using System.Collections.Generic;
using Smartstore.Core.Content.Widgets;

namespace Smartstore.Web.UI
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
        /// <param name="model">View model</param>
        /// <returns>A list of <see cref="WidgetInvoker"/> instances that should be injected into the zone.</returns>
        IEnumerable<WidgetInvoker> GetWidgets(string zone, object model = null);
    }
}
