#nullable enable

namespace Smartstore.Core.Localization;

/// <summary>
/// Represents a string whose value is already available as a literal.
/// </summary>
public sealed class LiteralText : ResolvableText
{
    public LiteralText(string value)
    {
        Guard.NotEmpty(value);
        Value = value;
    }

    /// <summary>
    /// Gets the literal text value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override LocalizedString Resolve(Localizer localizer)
        => Value;

    /// <inheritdoc />
    public override bool Equals(ResolvableText? other)
        => other is LiteralText literal && string.Equals(Value, literal.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(typeof(LiteralText), Value);

    /// <inheritdoc />
    public override string ToString()
        => Value;
}
