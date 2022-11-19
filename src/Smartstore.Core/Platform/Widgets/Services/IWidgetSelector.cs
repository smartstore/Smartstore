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
        /// <param name="zone">Zone name to resolve widgets for.</param>
        /// <param name="model">Optional view model</param>
        /// <returns>A list of <see cref="Widget"/> instances that should be injected into the zone.</returns>
        Task<IEnumerable<Widget>> GetWidgetsAsync(string zone, object model = null);
    }
}
