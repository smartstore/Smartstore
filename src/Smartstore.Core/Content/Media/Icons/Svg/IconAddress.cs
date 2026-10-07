#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// A persistent icon address in the form [library:]name[!][@variant].
/// </summary>
public readonly record struct IconAddress
{
    /// <summary>
    /// Creates an address. Missing qualifiers are resolved by the icon service.
    /// </summary>
    /// <param name="name">The exact icon or conceptual name without qualifiers, a bypass marker or a file extension. Whitespace and casing are preserved.</param>
    /// <param name="library">An optional library system name or short name, normalized to lowercase.</param>
    /// <param name="variant">An optional variant name or short name, normalized to lowercase.</param>
    /// <param name="skipMapping">Whether to address the icon directly instead of applying a conceptual mapping.</param>
    /// <exception cref="ArgumentException">A component is empty or contains characters forbidden in an icon address.</exception>
    public IconAddress(string name, string? library = null, string? variant = null, bool skipMapping = false)
    {
        if (!IsName(name))
        {
            throw new ArgumentException("Invalid icon name.", nameof(name));
        }

        if (library != null && !IsQualifier(library))
        {
            throw new ArgumentException("Invalid library selector.", nameof(library));
        }

        if (variant != null && !IsQualifier(variant))
        {
            throw new ArgumentException("Invalid variant selector.", nameof(variant));
        }

        SkipMapping = skipMapping;
        Name = name;
        Library = library?.ToLowerInvariant();
        Variant = variant?.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the exact icon or conceptual name.
    /// </summary>
    public string Name { get; private init; }

    /// <summary>
    /// Gets the optional library name.
    /// </summary>
    public string? Library { get; private init; }

    /// <summary>
    /// Gets the optional variant name.
    /// </summary>
    public string? Variant { get; private init; }

    /// <summary>
    /// Gets whether the name must be resolved directly, bypassing conceptual mappings.
    /// </summary>
    public bool SkipMapping { get; private init; }

    /// <summary>
    /// Gets whether this is the uninitialized default value, which cannot identify an icon.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <summary>
    /// Gets whether both qualifiers have been supplied.
    /// </summary>
    public bool IsQualified => Library != null && Variant != null;

    /// <summary>
    /// Parses an address without resolving defaults or mappings.
    /// </summary>
    /// <param name="value">An address in the form [library:]name[!][@variant].</param>
    /// <returns>The parsed address with any omitted selectors left unset.</returns>
    /// <exception cref="FormatException">The value is empty or has invalid address syntax.</exception>
    public static IconAddress Parse(string value)
    {
        if (!TryParse(value, out var address))
        {
            throw new FormatException($"Invalid icon address '{value}'.");
        }

        return address;
    }

    /// <summary>
    /// Attempts to parse an address without changing the icon name.
    /// </summary>
    /// <param name="value">The possibly null address text to validate.</param>
    /// <param name="address">The parsed address on success; otherwise the empty default value.</param>
    /// <returns>True when the value has valid address syntax; otherwise false.</returns>
    public static bool TryParse(string? value, out IconAddress address)
    {
        address = default;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var text = value.AsSpan();
        var colon = text.IndexOf(':');
        var at = text.IndexOf('@');
        if (at >= 0 && at < colon)
        {
            return false;
        }

        var nameStart = colon + 1;
        var nameEnd = at < 0 ? text.Length : at;
        var name = text[nameStart..nameEnd];
        var skipMapping = !name.IsEmpty && name[^1] == '!';
        if (skipMapping)
        {
            name = name[..^1];
        }
        var library = colon < 0 ? default : text[..colon];
        var variant = at < 0 ? default : text[(at + 1)..];

        // Validate spans before allocating substrings. Invalid input allocates nothing,
        // and the common unqualified name reuses the original string unchanged.
        if (!IsName(name) || (colon >= 0 && !IsQualifier(library)) || (at >= 0 && !IsQualifier(variant)))
        {
            return false;
        }

        // Validation is complete: bypass the public constructor's repeated scans.
        // Preserve exact source names, including casing and trailing spaces.
        address = new IconAddress
        {
            Name = colon < 0 && at < 0 && !skipMapping ? value : name.ToString(),
            SkipMapping = skipMapping,
            Library = colon < 0 ? null : NormalizeQualifier(value, 0, colon),
            Variant = at < 0 ? null : NormalizeQualifier(value, at + 1, value.Length - at - 1)
        };
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => IsEmpty ? string.Empty
        : SkipMapping
            ? Library == null
                ? Variant == null ? string.Concat(Name, "!") : $"{Name}!@{Variant}"
                : Variant == null ? $"{Library}:{Name}!" : $"{Library}:{Name}!@{Variant}"
        : Library == null
            ? Variant == null ? Name : string.Concat(Name, "@", Variant)
            : Variant == null ? string.Concat(Library, ":", Name)
            : $"{Library}:{Name}@{Variant}";

    /// <summary>
    /// Parses a string without resolving defaults or mappings. Invalid syntax throws a FormatException.
    /// </summary>
    /// <param name="value">An address in the form [library:]name[!][@variant].</param>
    /// <returns>The parsed address without configuration-dependent resolution.</returns>
    /// <exception cref="FormatException">The value has invalid address syntax.</exception>
    public static implicit operator IconAddress(string value) => Parse(value);

    /// <summary>
    /// Formats an address. The uninitialized default value produces an empty string.
    /// </summary>
    /// <param name="address">The address to format, including any supplied selectors.</param>
    /// <returns>The address text, or an empty string for the default value.</returns>
    public static implicit operator string(IconAddress address) => address.ToString();

    /// <summary>
    /// Validates a library or variant selector independently of installed configuration.
    /// </summary>
    /// <param name="value">A full name or short name containing only ASCII letters, digits, hyphens or underscores.</param>
    /// <returns>True when the selector is nonempty and contains only supported characters.</returns>
    internal static bool IsQualifier(string? value) => IsQualifier(value.AsSpan());

    /// <summary>
    /// Materializes a validated ASCII selector with at most one string allocation.
    /// </summary>
    /// <param name="source">The original address string.</param>
    /// <param name="start">The selector's starting index.</param>
    /// <param name="length">The selector's character count.</param>
    /// <returns>The lowercase selector.</returns>
    private static string NormalizeQualifier(string source, int start, int length)
    {
        // Already lowercase selectors need only the final substring. Uppercase selectors
        // are copied and folded directly into their result, without an intermediate string.
        foreach (var c in source.AsSpan(start, length))
        {
            if (char.IsAsciiLetterUpper(c))
            {
                return string.Create(length, (Source: source, Start: start), static (destination, state) =>
                {
                    var input = state.Source.AsSpan(state.Start, destination.Length);
                    for (var i = 0; i < input.Length; i++)
                    {
                        var character = input[i];
                        destination[i] = char.IsAsciiLetterUpper(character) ? (char)(character + ('a' - 'A')) : character;
                    }
                });
            }
        }

        return source.Substring(start, length);
    }

    /// <summary>
    /// Validates selector characters without substring or LINQ allocations.
    /// </summary>
    /// <param name="value">The selector slice to validate.</param>
    /// <returns>Whether the nonempty slice contains only supported ASCII characters.</returns>
    private static bool IsQualifier(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates an exact icon name while excluding address delimiters, paths and control characters.
    /// </summary>
    /// <param name="value">The icon or conceptual name without a file extension.</param>
    /// <returns>True when the name can be represented unambiguously in an address.</returns>
    internal static bool IsName(string? value) => IsName(value.AsSpan());

    /// <summary>
    /// Validates exact name characters and whitespace in one allocation-free scan.
    /// </summary>
    /// <param name="value">The unqualified icon name slice.</param>
    /// <returns>Whether the slice is a nonblank, non-path icon name.</returns>
    private static bool IsName(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty || value.SequenceEqual(".") || value.SequenceEqual(".."))
        {
            return false;
        }

        var hasContent = false;
        foreach (var c in value)
        {
            if (char.IsControl(c) || c is ':' or '@' or '!' or '/' or '\\')
            {
                return false;
            }

            hasContent |= !char.IsWhiteSpace(c);
        }

        return hasContent;
    }
}
