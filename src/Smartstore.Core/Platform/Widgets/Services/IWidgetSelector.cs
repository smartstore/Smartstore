#nullable enable

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Resolves widgets for zones.
    /// </summary>
    public interface IWidgetSelector
    {
        /// <summary>
        /// Enumerates all widgets for the given zone, unsorted.
        /// </summary>
        /// <param name="zone">Zone to enumerate widgets for.</param>
        /// <param name="model">Optional view model.</param>
        /// <returns>An enumeration of <see cref="Widget"/> instances that should be injected into the zone.</returns>
        IAsyncEnumerable<Widget> EnumerateWidgetsAsync(IWidgetZone zone, object? model = null);
    }
}
