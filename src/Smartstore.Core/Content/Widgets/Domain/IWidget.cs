using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Content.Widgets
{
    /// <summary>
    /// Provides an interface for creating widgets.
    /// </summary>
    public partial interface IWidget : IProvider, IUserEditable
    {
        /// <summary>
        /// Gets widget zones where this widget should be rendered.
        /// </summary>
        /// <returns>Widget zones</returns>
        IList<string> GetWidgetZones();

        /// <summary>
        /// Gets a route for displaying a widget.
        /// </summary>
        /// <param name="widgetZone">Widget zone where it's displayed.</param>
		/// <param name="model">The model of the parent view context.</param>
		/// <param name="storeId">The id of the current store.</param>
        WidgetInvoker GetDisplayWidget(string widgetZone, object model, int storeId);
    }
}
