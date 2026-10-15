using System.Collections.Concurrent;
using EmbeddedSass.Functions;
using EmbeddedSass.Values;
using Smartstore.IconKit;

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
            var expression = IconExpression.Parse(address);
            var icon = await icons.GetIconAsync(expression.Address, area: area, cancelToken: cancelToken);
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
                : source.ToStandaloneSvg(new IconTransform(expression.Presentation.FlipHorizontal ?? icon.Transform.FlipX,
                    expression.Presentation.FlipVertical ?? icon.Transform.FlipY, expression.Presentation.Rotate ?? icon.Transform.Rotation),
                    icon.SupportsStrokeScaling ? expression.Presentation.StrokeScale ?? icon.StrokeScale : 1, expression.Presentation.Color);

            return "data:image/svg+xml," + Uri.EscapeDataString(svg);
        }
    }
}
