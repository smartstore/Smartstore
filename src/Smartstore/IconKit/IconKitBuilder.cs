using System.Text.Json;

namespace Smartstore.IconKit;

/// <summary>
/// Collects additive provider definitions. Local JSON declarations retain precedence.
/// Supports existing library addresses and private SVG drawings without registering public libraries or variants.
/// </summary>
public sealed class IconKitBuilder
{
    internal Dictionary<string, IconKit> Kits { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, Dictionary<string, string>> KitIcons { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, Dictionary<string, string>> CollectionIcons { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, IconProviderDrawing> Drawings { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Defines a kit, or contributes its members to a locally configured kit of the same name.
    /// Local kit settings win; provider-only kits follow all JSON kits in provider type-name order.
    /// </summary>
    /// <param name="kit">The immutable kit definition. All sources must refer to installed libraries.</param>
    public IconSetBuilder AddKit(IconKit kit)
    {
        Guard.NotNull(kit);

        ValidateName(kit.Name);
        if (!Kits.TryAdd(kit.Name, kit))
        {
            throw new InvalidDataException($"Multiple icon providers define kit '{kit.Name}'. Use Kit to extend an existing kit.");
        }

        var target = Kit(kit.Name);
        foreach (var concept in kit.Icons)
        {
            target.Add(concept, kit.Sources.GetValueOrDefault(concept));
        }

        return target;
    }

    /// <summary>
    /// Selects a JSON kit or a kit declared with AddKit for fluent contributions.
    /// Unknown target kits are configuration errors.
    /// </summary>
    /// <param name="name">The target kit name.</param>
    public IconSetBuilder Kit(string name) => Select(KitIcons, name);

    /// <summary>
    /// Selects or creates a collection. Every including kit receives its contributions.
    /// Collections never produce a sprite of their own.
    /// </summary>
    /// <param name="name">The target collection name.</param>
    public IconSetBuilder Collection(string name) => Select(CollectionIcons, name);

    private IconSetBuilder Select(Dictionary<string, Dictionary<string, string>> definitions, string name)
    {
        ValidateName(name);
        if (!definitions.TryGetValue(name, out var icons))
        {
            icons = new(StringComparer.Ordinal);
            definitions.Add(name, icons);
        }

        return new IconSetBuilder(name, icons, Drawings);
    }

    private static void ValidateName(string name)
    {
        if (!IconAddress.IsQualifier(name))
        {
            throw new InvalidDataException($"Invalid icon kit or collection name '{name}'.");
        }
    }

    // Reuse the catalog's JSON validation and collection expansion for provider-only kit settings.
    internal static JsonElement GetDefinition(IconKit kit) => JsonSerializer.SerializeToElement(new
    {
        defaultLibrary = kit.DefaultLibrary,
        defaultVariant = kit.DefaultVariant,
        icon = kit.Icon,
        displayName = kit.DisplayName,
        showInPicker = kit.ShowInPicker,
        areas = kit.Areas
    });
}
