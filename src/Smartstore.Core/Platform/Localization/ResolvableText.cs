#nullable enable

namespace Smartstore.Core.Localization;

/// <summary>
/// Represents text whose translated value can be resolved at runtime.
/// </summary>
public abstract class ResolvableText : IEquatable<ResolvableText>
{
    /// <summary>
    /// Creates resolvable text containing a literal value.
    /// </summary>
    /// <param name="value">The literal text value.</param>
    /// <returns>The resolvable literal text.</returns>
    public static ResolvableText Literal(string value)
        => new LiteralText(value);

    /// <summary>
    /// Creates resolvable text referring to a localization resource.
    /// </summary>
    /// <param name="resourceKey">The localization resource key.</param>
    /// <returns>The resolvable resource text.</returns>
    public static ResolvableText Resource(string resourceKey)
        => new ResourceText(resourceKey);

    /// <summary>
    /// Resolves the localized text value.
    /// </summary>
    /// <param name="localizer">The localizer used to resolve resource-backed text.</param>
    /// <returns>The resolved localized text.</returns>
    public abstract LocalizedString Resolve(Localizer localizer);

    /// <summary>
    /// Determines whether this instance represents the same text source as another instance.
    /// </summary>
    /// <param name="other">The instance to compare.</param>
    /// <returns><see langword="true"/> when type and value are equal; otherwise, <see langword="false"/>.</returns>
    public abstract bool Equals(ResolvableText? other);

    /// <inheritdoc />
    public abstract override int GetHashCode();

    /// <inheritdoc />
    public abstract override string ToString();

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => Equals(obj as ResolvableText);

    /// <summary>
    /// Converts a literal string value to a <see cref="ResolvableText"/>.
    /// </summary>
    /// <param name="value">The literal string value.</param>
    /// <returns>The resolvable literal text.</returns>
    public static implicit operator ResolvableText(string value)
        => Literal(value);

    /// <summary>
    /// Determines whether two resolvable texts represent the same text source.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> when both values are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ResolvableText? left, ResolvableText? right)
        => EqualityComparer<ResolvableText>.Default.Equals(left, right);

    /// <summary>
    /// Determines whether two resolvable texts represent different text sources.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> when both values differ; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ResolvableText? left, ResolvableText? right)
        => !(left == right);
}
