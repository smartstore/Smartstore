namespace Smartstore.IconKit;

/// <summary>
/// Adds concepts to a selected kit or collection without changing its settings.
/// </summary>
public sealed class IconSetBuilder
{
    private readonly string _name;
    private readonly Dictionary<string, string> _icons;

    internal IconSetBuilder(string name, Dictionary<string, string> icons)
    {
        _name = name;
        _icons = icons;
    }

    /// <summary>
    /// Adds a concept. Identical contributions are idempotent; conflicting provider sources are errors.
    /// </summary>
    /// <param name="concept">The unqualified concept name.</param>
    /// <param name="source">Optional concrete source address. Omission uses normal library mapping.</param>
    public IconSetBuilder Add(string concept, string source = null)
    {
        if (!IconAddress.IsName(concept) || source != null && !IconAddress.TryParse(source, out _))
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
}
