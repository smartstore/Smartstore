#nullable enable

using System.Text;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Smartstore.IconKit;

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Converts legacy icon values independently of their storage and without resolving icon sources.
/// </summary>
public class LegacyIconConverter
{
    private readonly IReadOnlySet<string>? _brandIconNames;

    /// <summary>
    /// Creates a converter with optional legacy brand metadata supplied by the caller.
    /// No icon catalog or files are loaded by this utility.
    /// </summary>
    public LegacyIconConverter(IReadOnlySet<string>? brandIconNames = null)
    {
        _brandIconNames = brandIconNames;
    }

    /// <summary>
    /// Converts a Font Awesome name, style and optional theme color to an explicit expression.
    /// Already qualified expressions are returned unchanged.
    /// </summary>
    /// <param name="name">The legacy name, optionally prefixed with fa-.</param>
    /// <param name="style">The legacy style name or Font Awesome style class.</param>
    /// <param name="color">An optional legacy theme color name, such as primary or gray-500.</param>
    /// <returns>The expression and an optional conversion warning; the expression is null for empty or unsupported input.</returns>
    public (IconExpression? Expression, string? Warning) ConvertIcon(string? name, string? style = null, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, null);
        }

        // Repeated/manual runs must not reinterpret an already qualified expression as legacy data.
        if (IconExpression.TryParse(name, out var existing) && existing.Address.Library != null)
        {
            return (existing, null);
        }

        name = name.Trim();
        if (name.StartsWith("fa-", StringComparison.Ordinal))
        {
            name = name[3..];
        }

        // The legacy renderer always used Brands for known brand icons, even with no style or a stale style.
        if (_brandIconNames?.Contains(name) == true)
        {
            style = "brands";
        }

        var variant = style?.Trim().ToLowerInvariant() switch
        {
            null or "" or "fa" or "fas" or "solid" or "s" => "s",
            "far" or "regular" or "r" => "r",
            "fal" or "light" or "l" => "l",
            "fad" or "duotone" or "duotone-solid" or "ds" => "ds",
            "fab" or "brands" or "b" => "b",
            _ => null
        };

        if (variant == null)
        {
            return (null, $"Unsupported icon style '{style}'; the icon was not converted.");
        }

        // Legacy colors are theme tokens, not arbitrary CSS. An invalid color must not block the icon.
        var warning = (string?)null;
        color = color?.Trim();
        if (!string.IsNullOrEmpty(color) && color.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
        {
            warning = $"Theme color '{color}' could not be converted; the icon was converted without a color.";
            color = null;
        }

        var presentation = new IconPresentation
        {
            Color = string.IsNullOrEmpty(color) ? null : $"var(--{color})"
        };

        try
        {
            // Preserve the original FA source and style, bypass concept mappings and do not resolve SVGs.
            var address = new IconAddress(name, "fa", variant, skipMapping: true);
            return (new IconExpression(address, presentation), warning);
        }
        catch (ArgumentException)
        {
            return (null, $"Invalid legacy icon name '{name}'; the icon was not converted.");
        }
    }

    /// <summary>
    /// Replaces empty i elements whose class attribute contains fa- with sm-icon elements.
    /// Unrelated HTML and attributes remain unchanged; unsupported icons produce warnings.
    /// </summary>
    /// <param name="html">The source HTML.</param>
    /// <returns>The converted HTML, warnings and counts of converted and unsupported icon tags.</returns>
    public (string Html, IReadOnlyList<string> Warnings, int ConvertedIcons, int NotConvertedIcons) ConvertHtml(string html)
    {
        Guard.NotNull(html);

        // Avoid regex work and output allocation for the common case without legacy icons.
        if (!html.Contains("fa-", StringComparison.Ordinal))
        {
            return (html, Array.Empty<string>(), 0, 0);
        }

        var warnings = new List<string>();
        var output = (StringBuilder?)null;
        var copiedUntil = 0;
        var convertedIcons = 0;
        var notConvertedIcons = 0;
        for (var match = LegacyHtmlIcon.Match(html); match.Success; match = match.NextMatch())
        {
            var converted = ConvertClasses(WebUtility.HtmlDecode(match.Groups["classes"].Value));
            if (converted.Warning != null)
            {
                warnings.Add($"{converted.Warning} at offset {match.Index}.");
            }

            if (converted.Expression is not { } expression)
            {
                notConvertedIcons++;
                continue;
            }

            // Allocate on the first replacement and copy untouched spans verbatim, including skipped icons.
            output ??= new StringBuilder(html.Length);
            output.Append(html, copiedUntil, match.Index - copiedUntil);
            output.Append("<sm-icon");
            output.Append(match.Groups["before"].ValueSpan);
            output.Append(match.Groups["after"].ValueSpan);
            if (converted.RemainingClasses.Length > 0)
            {
                output.Append(" class=\"");
                output.Append(WebUtility.HtmlEncode(converted.RemainingClasses));
                output.Append('"');
            }

            // Emit readable component attributes rather than embedding the presentation in a query string.
            // The ! suffix still ensures that the original icon name bypasses concept mappings.
            AppendAttribute("name", expression.Address.Name + (expression.Address.SkipMapping ? "!" : ""));
            AppendAttribute("lib", expression.Address.Library);
            AppendAttribute("variant", expression.Address.Variant);
            var presentation = expression.Presentation;
            AppendAttribute("size", presentation.Size);
            AppendAttribute("fw", presentation.FixedWidth);
            AppendAttribute("inverse", presentation.Inverse);
            AppendAttribute("rotate", presentation.Rotate);
            AppendAttribute("flip-h", presentation.FlipHorizontal);
            AppendAttribute("flip-v", presentation.FlipVertical);
            AppendAttribute("animation", presentation.Animation);
            AppendAttribute("animation-reverse", presentation.AnimationReverse);
            output.Append("></sm-icon>");

            void AppendAttribute(string name, object? value)
            {
                if (value == null)
                {
                    return;
                }

                var text = value is bool flag ? (flag ? "true" : "false") : Convert.ToString(value, CultureInfo.InvariantCulture);
                output.Append(' ').Append(name).Append("=\"");
                output.Append(WebUtility.HtmlEncode(text));
                output.Append('"');
            }

            copiedUntil = match.Index + match.Length;
            convertedIcons++;
        }

        if (output == null)
        {
            return (html, warnings, 0, notConvertedIcons);
        }

        output.Append(html, copiedUntil, html.Length - copiedUntil);
        return (output.ToString(), warnings, convertedIcons, notConvertedIcons);
    }

    /// <summary>
    /// Converts legacy Font Awesome classes to an expression and preserves unrelated classes separately.
    /// </summary>
    /// <param name="classes">The decoded class attribute value.</param>
    /// <returns>The expression, remaining classes and an optional warning; the expression is null for unsupported input.</returns>
    public (IconExpression? Expression, string RemainingClasses, string? Warning) ConvertClasses(string classes)
    {
        Guard.NotNull(classes);

        var tokens = classes.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var style = "s";
        var names = new List<string>();
        var retainedClasses = new List<string>();
        var presentation = new IconPresentation();
        var unsupported = false;

        // Consume known FA styles/modifiers once; leave unrelated classes such as text-primary intact.
        foreach (var token in tokens)
        {
            switch (token)
            {
                case "fa": case "fas": case "fa-solid": style = "s"; break;
                case "far": case "fa-regular": style = "r"; break;
                case "fal": case "fa-light": style = "l"; break;
                case "fad": case "fa-duotone": style = "ds"; break;
                case "fab": case "fa-brands": style = "b"; break;
                case "fa-fw": presentation = presentation with { FixedWidth = true }; break;
                case "fa-inverse": presentation = presentation with { Inverse = true }; break;
                case "fa-spin": presentation = presentation with { Animation = "spin" }; break;
                case "fa-pulse": presentation = presentation with { Animation = "pulse" }; break;
                case "fa-spin-reverse": presentation = presentation with { AnimationReverse = true }; break;
                case "fa-rotate-90": presentation = presentation with { Rotate = 90 }; break;
                case "fa-rotate-180": presentation = presentation with { Rotate = 180 }; break;
                case "fa-rotate-270": presentation = presentation with { Rotate = 270 }; break;
                case "fa-flip-horizontal": presentation = presentation with { FlipHorizontal = true }; break;
                case "fa-flip-vertical": presentation = presentation with { FlipVertical = true }; break;
                case "fa-flip-both": presentation = presentation with { FlipHorizontal = true, FlipVertical = true }; break;
                case "fa-2xs": case "fa-xs": case "fa-sm": case "fa-lg": case "fa-xl": case "fa-2xl":
                case "fa-1x": case "fa-2x": case "fa-3x": case "fa-4x": case "fa-5x":
                case "fa-6x": case "fa-7x": case "fa-8x": case "fa-9x": case "fa-10x":
                    presentation = presentation with { Size = token[3..] };
                    break;
                case "fa-stack": case "fa-stack-1x": case "fa-stack-2x":
                case "fa-li": case "fa-border": case "fa-pull-left": case "fa-pull-right":
                case "fa-rotate-by":
                    unsupported = true;
                    break;
                default:
                    if (token.StartsWith("fa-", StringComparison.Ordinal))
                    {
                        names.Add(token);
                    }
                    else
                    {
                        retainedClasses.Add(token);
                    }
                    break;
            }
        }

        // Ambiguous names and unsupported layout modifiers stay unchanged instead of losing appearance.
        if (unsupported || names.Count != 1)
        {
            return (null, classes, $"Unsupported icon classes: {classes}");
        }

        var converted = ConvertIcon(names[0], style);
        if (converted.Expression is not { } expression)
        {
            return (null, classes, converted.Warning);
        }

        return (new IconExpression(expression.Address, presentation), string.Join(" ", retainedClasses), converted.Warning);
    }

    // Deliberately target empty i tags with fa- in a quoted class attribute, not arbitrary HTML.
    // before/after preserve other attributes; this is not a parser and does not exclude comments or scripts.
    private static readonly Regex LegacyHtmlIcon = new(
        """<i\b(?<before>[^>]*?)\sclass\s*=\s*(?:"(?<classes>[^"]*fa-[^"]*)"|'(?<classes>[^']*fa-[^']*)')(?<after>[^>]*)>\s*</i\s*>""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(2));
}
