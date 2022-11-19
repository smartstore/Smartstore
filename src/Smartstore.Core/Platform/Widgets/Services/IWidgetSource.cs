namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Provides <see cref="Widget"/> instances for particular widget zones. 
    /// </summary>
    public interface IWidgetSource
    {
        int Order { get; }
        
        /// <summary>
        /// Enumerates all widgets for a given zone.
        /// </summary>
        /// <param name="zone">Zone name to retrieve widgets for.</param>
        /// <param name="isPublicArea">Indicates whether the current route endpoint is in the public store frontend.</param>
        /// <param name="model">Optional view model</param>
        /// <returns>List of <see cref="Widget"/> instances.</returns>
        Task<IEnumerable<Widget>> GetWidgetsAsync(string zone, bool isPublicArea, object model = null);
    }
}
