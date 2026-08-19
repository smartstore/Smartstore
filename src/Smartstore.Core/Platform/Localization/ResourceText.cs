#nullable enable

namespace Smartstore.Core.Localization;

/// <summary>
/// Represents resolvable text whose value is obtained from a localization resource.
/// </summary>
public sealed class ResourceText : ResolvableText
{
    public ResourceText(string resourceKey)
    {
        Guard.NotEmpty(resourceKey);
        ResourceKey = resourceKey;
    }

    /// <summary>
    /// Gets the localization resource key.
    /// </summary>
    public string ResourceKey { get; }

    /// <inheritdoc />
    public override LocalizedString Resolve(Localizer localizer)
        => localizer(ResourceKey);

    /// <inheritdoc />
    public override bool Equals(ResolvableText? other)
        => other is ResourceText resource && string.Equals(ResourceKey, resource.ResourceKey, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(typeof(ResourceText), ResourceKey);

    /// <inheritdoc />
    public override string ToString()
        => ResourceKey;
}
