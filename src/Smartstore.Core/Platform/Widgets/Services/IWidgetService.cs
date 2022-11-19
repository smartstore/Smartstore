using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Widgets
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
		IEnumerable<Provider<IActivatableWidget>> LoadActiveWidgets(int storeId = 0);

        /// <summary>
        /// Load active widgets
        /// </summary>
        /// <param name="widgetZone">Widget zone</param>
        /// <param name="storeId">Load records allows only in specified store; pass 0 to load all records</param>
        /// <returns>Widgets</returns>
        IEnumerable<Provider<IActivatableWidget>> LoadActiveWidgetsByWidgetZone(string widgetZone, int storeId = 0);

        /// <summary>
        /// (De)activates a global widget.
        /// </summary>
        /// <param name="systemName">System name of widget to (de)activate.</param>
        /// <param name="activate"><c>true</c>: activates widget, <c>false:</c> deactivates widget.</param>
        Task ActivateWidgetAsync(string systemName, bool activate);
    }
}
