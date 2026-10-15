#nullable enable

namespace Smartstore.IconKit;

/// <summary>
/// An immutable icon address and presentation that round-trips through a persistent string.
/// Omitted values remain distinct from explicit resets. No library resolution occurs here.
/// </summary>
public readonly record struct IconExpression
{
    /// <summary>
    /// Creates an expression with an icon identity and optional presentation.
    /// </summary>
    /// <param name="address">The non-empty icon address.</param>
    /// <param name="presentation">The presentation values to persist with the address.</param>
    public IconExpression(IconAddress address, IconPresentation presentation = default)
    {
        if (address.IsEmpty)
        {
            throw new ArgumentException("An icon address is required.", nameof(address));
        }

        Address = address;
        Presentation = presentation;
    }

    /// <summary>
    /// Gets the unresolved icon identity. Only the default expression has an empty address.
    /// </summary>
    public IconAddress Address { get; }

    /// <summary>
    /// Gets the icon's presentation values.
    /// </summary>
    public IconPresentation Presentation { get; init; }

    /// <summary>
    /// Parses an address and its optional presentation modifiers without resolving mappings.
    /// </summary>
    /// <remarks>
    /// Empty and invalid known modifiers are ignored; the last valid value wins.
    /// Unknown keys and unsafe CSS delimiters are rejected to prevent silent data loss or declaration injection.
    /// </remarks>
    /// <param name="value">The persistent icon expression.</param>
    public static IconExpression Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var separator = value.IndexOf('?');
        if (!IconAddress.TryParse(value, separator < 0 ? value.Length : separator, out var address))
        {
            throw new FormatException($"Invalid icon address in '{value}'.");
        }
        return new IconExpression(address, separator < 0 ? default : IconPresentation.Parse(value.AsSpan(separator)));
    }

    /// <summary>
    /// Attempts to parse a persistent expression; invalid input returns the empty default value.
    /// </summary>
    /// <param name="value">The expression to validate.</param>
    /// <param name="expression">The parsed value on success.</param>
    public static bool TryParse(string? value, out IconExpression expression)
    {
        expression = default;
        if (string.IsNullOrEmpty(value)) return false;
        var separator = value.IndexOf('?');
        IconPresentation presentation = default;
        if (!IconAddress.TryParse(value, separator < 0 ? value.Length : separator, out var address)
            || (separator >= 0 && !IconPresentation.TryParse(value.AsSpan(separator), out presentation)))
        {
            expression = default;
            return false;
        }

        expression = new IconExpression(address, presentation);
        return true;
    }

    /// <summary>
    /// Converts a string explicitly, reporting invalid syntax instead of silently losing modifiers.
    /// </summary>
    public static explicit operator IconExpression(string value) => Parse(value);

    /// <summary>
    /// Converts an expression to its persistent representation.
    /// </summary>
    public static implicit operator string(IconExpression value) => value.ToString();

    /// <summary>
    /// Formats a culture-independent expression. Reserved query delimiters are escaped; CSS colors remain readable.
    /// </summary>
    public override string ToString()
    {
        if (Address.IsEmpty)
        {
            if (this == default) return string.Empty;
            throw new InvalidOperationException("An icon address is required to persist an expression.");
        }

        return Address.ToString() + Presentation.ToString();
    }
}
