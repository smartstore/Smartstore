using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Smartstore.Core.Content.Media.Icons;

// Converts source XML into a cacheable payload: validate and extract
// root attributes/child markup. Per-render sizing, animation and transforms do not belong here.
/// <summary>
/// Validates static SVG sources and prepares serializable payloads with configurable stroke fallbacks and CSS variable overrides.
/// </summary>
internal static class IconSvgParser
{
    /// <summary>
    /// The per-icon source byte limit and XML reader character limit.
    /// </summary>
    internal const int MaxLength = 1024 * 1024;

    // Increment when preparation rules change, independently of library package versions.
    /// <summary>
    /// The preparation format version included in cache revisions independently of source content.
    /// </summary>
    internal const string Revision = "13";
    private const string _svgNamespace = "http://www.w3.org/2000/svg";
    // Support static icon graphics only. This is deliberately not a general SVG document
    // renderer: executable content, external resources and arbitrary source CSS are excluded.
    private static readonly HashSet<string> _elements = new(StringComparer.Ordinal)
    {
        "svg", "g", "path", "circle", "ellipse", "rect", "line", "polyline", "polygon", "title", "desc",
        "defs", "clipPath", "mask", "linearGradient", "radialGradient", "stop", "use", "symbol"
    };
    private static readonly HashSet<string> _attributes = new(StringComparer.Ordinal)
    {
        "id", "class", "role", "aria-hidden", "aria-label", "aria-labelledby", "focusable",
        "viewBox", "width", "height", "preserveAspectRatio", "d", "x", "y", "x1", "x2", "y1", "y2",
        "cx", "cy", "r", "rx", "ry", "points", "transform", "fill", "fill-rule", "fill-opacity", "stroke",
        "stroke-width", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-dasharray", "stroke-dashoffset",
        "stroke-opacity", "opacity", "clip-rule", "clip-path", "clipPathUnits", "mask", "maskUnits", "maskContentUnits",
        "gradientUnits", "gradientTransform", "spreadMethod", "offset", "stop-color", "stop-opacity", "href",
        "fx", "fy", "fr", "vector-effect", "color", "paint-order", "shape-rendering"
    };

    /// <summary>
    /// Parses an SVG, validates supported content and extracts a presentation-independent payload.
    /// </summary>
    /// <param name="stream">The SVG source stream. It remains open and is consumed from its current position.</param>
    /// <param name="info">The resolved icon identity after conceptual mapping.</param>
    /// <param name="variant">The native grid fallback, optional stroke color and source stroke width multiplier.</param>
    /// <param name="revision">The combined preparation and source revision to embed in the payload.</param>
    /// <returns>A detached SVG payload containing root attributes and prepared child markup.</returns>
    /// <exception cref="XmlException">The source is malformed XML or violates XML reader restrictions.</exception>
    /// <exception cref="InvalidDataException">The source contains unsupported SVG content, coordinates or paint values.</exception>
    internal static IconSvg Parse(Stream stream, IconInfo info, IconVariant variant, string revision)
    {
        // Read directly from the selected ZIP entry or override. Only this icon's XML tree
        // is materialized; no intermediate SVG byte buffer or archive buffer is needed.
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxLength,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        });
        var document = XDocument.Load(reader);
        var root = document.Root;
        if (root == null || root.Name.LocalName != "svg")
        {
            throw new InvalidDataException($"Icon '{info.Address}' has no SVG root.");
        }

        // Validate static SVG content before extracting it. Presentation changes are applied only after validation.
        foreach (var element in root.DescendantsAndSelf())
        {
            if (!_elements.Contains(element.Name.LocalName)
                || (element.Name.NamespaceName != string.Empty && element.Name.NamespaceName != _svgNamespace))
            {
                throw new InvalidDataException($"Unsupported SVG element '{element.Name}' in '{info.Address}'.");
            }

            foreach (var attribute in element.Attributes().Where(x => !x.IsNamespaceDeclaration))
            {
                var name = attribute.Name.LocalName;
                var value = attribute.Value;
                // References may target IDs in this SVG only. CSS escapes and comments are
                // rejected so they cannot disguise resource references from these checks.
                if (!_attributes.Contains(name)
                    || attribute.Name.NamespaceName != string.Empty && !(name == "href" && attribute.Name.NamespaceName == "http://www.w3.org/1999/xlink")
                    || name == "href" && !Regex.IsMatch(value, "^#[A-Za-z_][A-Za-z0-9_.:-]*$")
                    || value.Contains('\\') || value.Contains("/*", StringComparison.Ordinal)
                    || value.Contains("url", StringComparison.OrdinalIgnoreCase) && !Regex.IsMatch(value, "^url\\(#[A-Za-z_][A-Za-z0-9_.:-]*\\)$"))
                {
                    throw new InvalidDataException($"Unsupported SVG attribute '{attribute.Name}' in '{info.Address}'.");
                }
            }
        }

        // Preserve the source coordinate system, including offsets and non-square dimensions.
        // GridSize is only a fallback when viewBox is missing, not a replacement for valid data.
        var viewBox = (string)root.Attribute("viewBox") ?? $"0 0 {variant.GridSize} {variant.GridSize}";
        var coordinates = viewBox.Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (coordinates.Length != 4 || coordinates.Any(x => !double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) || !double.IsFinite(n))
            || double.Parse(coordinates[2], CultureInfo.InvariantCulture) <= 0 || double.Parse(coordinates[3], CultureInfo.InvariantCulture) <= 0)
        {
            throw new InvalidDataException($"Invalid viewBox in '{info.Address}'.");
        }

        // Supply SVG's initial width so CSS overrides also reach strokes without an explicit width.
        if (root.Attribute("stroke-width") == null)
        {
            root.SetAttributeValue("stroke-width", "1");
        }

        // Keep the root free of stroke attributes without losing inherited source values.
        // Moving defaults one level down preserves descendant overrides and group inheritance.
        foreach (var attribute in root.Attributes()
            .Where(x => x.Name.LocalName == "stroke" || x.Name.LocalName.StartsWith("stroke-", StringComparison.Ordinal))
            .ToArray())
        {
            foreach (var child in root.Elements())
            {
                if (child.Attribute(attribute.Name) == null || (string)child.Attribute(attribute.Name) == "inherit")
                {
                    child.SetAttributeValue(attribute.Name, attribute.Value);
                }
            }

            attribute.Remove();
        }

        foreach (var element in root.Descendants())
        {
            var stroke = element.Attribute("stroke");
            if (stroke != null && !stroke.Value.Equals("none", StringComparison.OrdinalIgnoreCase)
                && !stroke.Value.Equals("inherit", StringComparison.OrdinalIgnoreCase))
            {
                // A missing manifest override preserves the source color. Deliberately
                // unstroked shapes stay unstroked even when the caller sets --icon-stroke.
                var fallback = variant.Stroke ?? stroke.Value;
                AppendStrokeStyle(element, "stroke", fallback);
            }

            // Scale declarations, not effective values: children inherit the already scaled
            // parent width, while explicit child widths are multiplied independently once.
            var width = element.Attribute("stroke-width");
            if (width != null && width.Value != "inherit")
            {
                if (variant.StrokeWidthScale != 1)
                {
                    width.Value = ScaleStrokeWidth(width.Value, variant.StrokeWidthScale);
                }

                AppendStrokeStyle(element, "stroke-width", width.Value);
            }
        }

        // CSS owns the rendered dimensions; viewBox retains the artwork coordinate system.
        root.Attribute("width")?.Remove();
        root.Attribute("height")?.Remove();

        // Normalize namespaces so child markup can be inserted into any normal SVG root.
        foreach (var element in root.DescendantsAndSelf())
        {
            element.Name = element.Name.LocalName;
            foreach (var attribute in element.Attributes().Where(x => x.IsNamespaceDeclaration).ToArray())
            {
                attribute.Remove();
            }

            var href = element.Attribute(XName.Get("href", "http://www.w3.org/1999/xlink"));
            if (href != null)
            {
                element.SetAttributeValue("href", href.Value);
                href.Remove();
            }
        }

        // Store data, not a live XML tree or a complete HTML element. The renderer supplies the
        // outer SVG namespace and must encode root attribute values.
        return new IconSvg
        {
            Address = info.Address,
            Library = info.LibraryName,
            Variant = info.VariantName,
            Name = info.Name,
            Revision = revision,
            ViewBox = viewBox,
            RootAttributes = root.Attributes().Where(x => x.Name.LocalName != "viewBox")
                .ToDictionary(x => x.Name.LocalName, x => x.Value),
            Content = string.Concat(root.Nodes().Select(x => x.ToString(SaveOptions.DisableFormatting)))
        };
    }

    /// <summary>
    /// Replaces a presentation attribute with a CSS variable declaration retaining its prepared fallback.
    /// </summary>
    /// <param name="element">The element declaring the stroke property.</param>
    /// <param name="property">The stroke property controlled by the corresponding icon variable.</param>
    /// <param name="fallback">The configured or original value used when the CSS variable is absent.</param>
    /// <exception cref="InvalidDataException">The fallback contains CSS declaration delimiters.</exception>
    private static void AppendStrokeStyle(XElement element, string property, string fallback)
    {
        // Source styles are rejected during validation. Do not let an attribute or manifest
        // value introduce additional declarations when it is embedded in our generated CSS.
        if (fallback.IndexOfAny([';', '{', '}', '<', '>', '\\']) >= 0
            || fallback.Any(char.IsControl) || fallback.Contains("/*", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Invalid SVG {property} fallback '{fallback}'.");
        }

        var style = (string)element.Attribute("style") ?? string.Empty;
        element.SetAttributeValue("style", style + $"{property}:var(--icon-{property},{fallback});");
        // The fallback now lives in CSS; retaining the presentation attribute would duplicate it.
        element.Attribute(property)?.Remove();
    }

    /// <summary>
    /// Scales a numeric SVG stroke width while retaining its unit or percentage suffix.
    /// </summary>
    /// <param name="value">The original width declaration.</param>
    /// <param name="scale">The non-negative multiplier from the variant manifest.</param>
    /// <returns>The scaled width formatted independently of the current culture.</returns>
    /// <exception cref="InvalidDataException">The width cannot be scaled as a finite numeric SVG length.</exception>
    private static string ScaleStrokeWidth(string value, double scale)
    {
        var match = Regex.Match(value.Trim(), @"^([+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?)(%|px|em|ex|ch|rem|cm|mm|in|pt|pc|Q)?$");
        if (!match.Success
            || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double width)
            || width < 0 || !double.IsFinite(width * scale))
        {
            throw new InvalidDataException($"Cannot scale SVG stroke width '{value}'.");
        }

        // Avoid exposing binary floating-point artifacts in the generated markup.
        return (width * scale).ToString("0.########", CultureInfo.InvariantCulture) + match.Groups[2].Value;
    }

}
