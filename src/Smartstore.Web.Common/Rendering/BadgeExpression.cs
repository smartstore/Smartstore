namespace Smartstore.Web.Rendering;

/// <summary>
/// Converts badge expressions into CSS classes for server-rendered badges.
/// </summary>
public static class BadgeExpression
{
    private static readonly string[] _colors = ["primary", "secondary", "success", "info", "warning", "danger", "light", "dark"];

    private static readonly IReadOnlyList<string> _availableColors = Array.AsReadOnly(_colors);

    /// <summary>
    /// Gets the available badge colors as a shared read-only list.
    /// </summary>
    public static IReadOnlyList<string> GetColors() => _availableColors;

    /// <summary>
    /// Creates a badge expression from a color and optional modifiers.
    /// </summary>
    /// <param name="color">The badge color, such as primary, secondary, or warning.</param>
    /// <param name="subtle">Whether to include the subtle modifier.</param>
    /// <param name="ring">Whether to include the ring modifier.</param>
    /// <param name="pill">Whether to include the pill modifier.</param>
    /// <param name="outline">Whether to include the outline modifier.</param>
    public static string Create(string color, bool subtle = false, bool ring = false, bool pill = false, bool outline = false)
    {
        return $"{color}{(subtle ? " subtle" : "")}{(outline ? " outline" : "")}{(ring ? " ring" : "")}{(pill ? " pill" : "")}";
    }

    /// <summary>
    /// Gets color and modifier classes, using secondary when no color is specified.
    /// </summary>
    public static string ToCssClasses(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return "badge badge-secondary";
        }

        var tokens = expression.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        var color = "secondary";
        foreach (var candidate in _colors)
        {
            if (Contains(tokens, candidate))
            {
                color = candidate;
                break;
            }
        }

        var outline = Contains(tokens, "outline");
        return $"badge badge-{color}{(!outline && Contains(tokens, "subtle") ? " badge-subtle" : "")}"
            + $"{(outline ? " badge-outline" : "")}{(Contains(tokens, "ring") ? " badge-ring" : "")}"
            + $"{(Contains(tokens, "pill") ? " badge-pill" : "")}";
    }

    // Expressions contain only a few tokens; linear comparison avoids a per-call hash set.
    private static bool Contains(string[] tokens, string value)
    {
        foreach (var token in tokens)
        {
            if (token.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
