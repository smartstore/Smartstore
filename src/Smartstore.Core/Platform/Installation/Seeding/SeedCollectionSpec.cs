namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Describes how a collection of seed records is merged: which field is the record key
/// and which fields are nested child collections that are merged by their own key.
/// </summary>
public sealed class SeedCollectionSpec
{
    private readonly Dictionary<string, SeedCollectionSpec> _children = new(StringComparer.OrdinalIgnoreCase);

    public SeedCollectionSpec(string keyField)
    {
        Guard.NotEmpty(keyField);
        KeyField = keyField;
    }

    /// <summary>
    /// The JSON field that identifies a record within its collection, e.g. "sku" or "twoLetterIsoCode".
    /// </summary>
    public string KeyField { get; }

    /// <summary>
    /// Nested child collections by JSON field name.
    /// </summary>
    public IReadOnlyDictionary<string, SeedCollectionSpec> Children => _children;

    public SeedCollectionSpec WithChild(string field, SeedCollectionSpec spec)
    {
        Guard.NotEmpty(field);
        Guard.NotNull(spec);

        _children[field] = spec;
        return this;
    }
}
