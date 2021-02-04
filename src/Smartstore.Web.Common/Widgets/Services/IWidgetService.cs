using System.Collections.Generic;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Widgets
{
    /// <summary>
    /// Widget service interface
    /// </summary>
    public partial interface IWidgetService
    {
        /// <summary>
        /// Load active widgets
        /// </summary>
		/// <param name="storeId">Load records allows only in specified store; pass 0 to load all records</param>
        /// <returns>Widgets</returns>
		IEnumerable<Provider<IWidget>> LoadActiveWidgets(int storeId = 0);


        /// <summary>
        /// Load active widgets
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="storeId">Load records allows only in specified store; pass 0 to load all records</param>
        /// <returns>Widgets</returns>
        IEnumerable<Provider<IWidget>> LoadActiveWidgetsByWidgetZone(string widgetZone, int storeId = 0);
    }
}
