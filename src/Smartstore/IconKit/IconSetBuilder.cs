namespace Smartstore.IconKit;

/// <summary>
/// Adds concepts to a selected kit or collection without changing its settings.
/// </summary>
public sealed class IconSetBuilder
{
    private readonly string _name;
    private readonly Dictionary<string, string> _icons;
    private readonly Dictionary<string, IconProviderDrawing> _drawings;

    internal IconSetBuilder(string name, Dictionary<string, string> icons, Dictionary<string, IconProviderDrawing> drawings)
    {
        _name = name;
        _icons = icons;
        _drawings = drawings;
    }

    /// <summary>
    /// Adds a concept. Identical contributions are idempotent; conflicting provider sources are errors.
    /// </summary>
    /// <param name="concept">The unqualified concept name.</param>
    /// <param name="source">Optional concrete source expression with mapping transforms. Omission uses normal library mapping.</param>
    public IconSetBuilder Add(string concept, string source = null)
    {
        if (!IconAddress.IsName(concept) || source != null && !IconExpression.TryParse(source, out _))
        {
            throw new InvalidDataException($"Invalid icon contribution '{concept}' to '{_name}'.");
        }

        if (_icons.TryGetValue(concept, out var existing) && existing != source)
        {
            throw new InvalidDataException($"Conflicting provider sources for icon '{concept}' in '{_name}'.");
        }

        _icons[concept] = source;
        return this;
    }

    /// <summary>
    /// Adds a concept backed by SVG markup. Parsing and sanitization are deferred until the drawing is needed.
    /// </summary>
    /// <param name="concept">The unqualified concept name.</param>
    /// <param name="svg">The complete SVG document, including its viewBox.</param>
    public IconSetBuilder AddSvg(string concept, string svg)
    {
        Guard.NotEmpty(svg);

        if (System.Text.Encoding.UTF8.GetByteCount(svg) > IconSvgParser.MaxLength)
        {
            throw new InvalidDataException("Provider SVG exceeds the supported size limit.");
        }

        return AddDrawing(concept, new IconProviderDrawing(svg, false));
    }

    /// <summary>
    /// Adds a concept backed by a local application file. File access is deferred. Changes are picked up when the catalog is rebuilt.
    /// </summary>
    /// <param name="concept">The unqualified concept name.</param>
    /// <param name="appPath">An application-relative SVG path, for example ~/Modules/MyPlugin/wwwroot/icons/logo.svg.</param>
    public IconSetBuilder AddSvgFile(string concept, string appPath)
    {
        Guard.NotEmpty(appPath);

        if (!appPath.StartsWith("~/", StringComparison.Ordinal)
            || !appPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || appPath.IndexOfAny(['\\', ':', '*', '?', '\0']) >= 0
            || appPath[2..].Split('/').Any(x => x is "" or "." or ".."))
        {
            throw new ArgumentException("An SVG file requires an application-relative path beginning with ~/ without traversal segments.", nameof(appPath));
        }

        return AddDrawing(concept, new IconProviderDrawing(appPath[2..], true));
    }

    private IconSetBuilder AddDrawing(string concept, IconProviderDrawing drawing)
    {
        var name = drawing.GetName();
        Add(concept, IconProviderFiles.LibraryName + ":" + name + "!/default");
        _drawings.TryAdd(name, drawing);
        return this;
    }
}
