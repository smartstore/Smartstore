using System.Collections.Concurrent;
using EmbeddedSass.Functions;
using EmbeddedSass.Values;
using Smartstore.Core.Content.Media.Icons;

namespace Smartstore.Web.Sass;

internal static class IconSassFunctions
{
    // Each compilation owns its callbacks and cache. Concurrent calls for the same
    // address share preparation without retaining results across catalog changes.
    internal static SassFunction CreateUrl(IIconService icons, string area = "public")
    {
        var urls = new ConcurrentDictionary<string, Lazy<Task<string>>>(StringComparer.Ordinal);

        return new SassFunction("icon-url($address)", async (arguments, cancelToken) =>
        {
            if (arguments[0] is not SassStringValue address || string.IsNullOrWhiteSpace(address.Text))
            {
                throw new ArgumentException("icon-url requires a non-empty icon address string.");
            }

            var url = await urls.GetOrAdd(address.Text, value => new Lazy<Task<string>>(
                () => CreateUrlAsync(value, cancelToken))).Value;

            return new SassStringValue($"url(\"{url}\")", false);
        });

        async Task<string> CreateUrlAsync(string address, CancellationToken cancelToken)
        {
            const string placeholderAddress = "system:not-found!/default";
            var icon = await icons.GetIconAsync(address, area: area, cancelToken: cancelToken);
            var source = icon == null ? null : await icons.GetSvgAsync(icon, cancelToken);
            if (source == null && address != placeholderAddress)
            {
                icon = await icons.GetIconAsync(placeholderAddress, area: area, cancelToken: cancelToken);
                source = icon == null ? null : await icons.GetSvgAsync(icon, cancelToken);
            }

            // An empty SVG keeps the mask transparent if even the placeholder is missing.
            // Returning 'none' would paint the entire pseudo-element in its background color.
            var svg = source == null
                ? "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"/>"
                : source.ToStandaloneSvg(icon.Transform, icon.SupportsStrokeScaling ? icon.StrokeScale : 1);

            return "data:image/svg+xml," + Uri.EscapeDataString(svg);
        }
    }
}
