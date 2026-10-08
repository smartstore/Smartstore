using System.Globalization;
using Microsoft.AspNetCore.Html;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Owns SVG markup and presentation for both external symbols and inline sources.
/// </summary>
/// <param name="icons">Provides validated inline payloads for icons outside kits.</param>
/// <param name="kits">Selects a versioned external symbol without generating its sprite.</param>
public sealed partial class IconRenderer(IIconService icons, IIconKitService kits) : IIconRenderer
{
    /// <inheritdoc />
    public async Task<TagBuilder> RenderAsync(IconInfo icon, IconOptions options = null, CancellationToken cancelToken = default)
    {
        Guard.NotNull(icon);

        cancelToken.ThrowIfCancellationRequested();
        var transform = new IconTransform(options?.FlipHorizontal ?? icon.Transform.FlipX,
            options?.FlipVertical ?? icon.Transform.FlipY, options?.Rotate ?? icon.Transform.Rotation);
        var strokeScale = options?.StrokeScale ?? icon.StrokeScale;
        if (!double.IsFinite(strokeScale) || strokeScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Stroke scale must be finite and positive.");
        }

        // Kit symbols already contain mapping transforms. Replacing one requires the
        // original source, not another transform around the existing symbol.
        var reference = icon.RequiresInline || transform != icon.Transform || strokeScale != 1
            ? null
            : kits.GetReference(icon);
        var svg = new TagBuilder("svg");
        if (reference != null)
        {
            // The external symbol supplies its viewBox; CSS sizes the use viewport.
            var use = new TagBuilder("use");
            use.Attributes["href"] = reference.Href;
            svg.InnerHtml.AppendHtml(use);
        }
        else
        {
            var source = await icons.GetSvgAsync(icon, cancelToken);
            if (source == null)
            {
                return null;
            }

            foreach (var attribute in source.RootAttributes)
            {
                svg.Attributes[attribute.Key] = attribute.Value;
            }
            svg.Attributes["viewBox"] = source.ViewBox;
            // Only the inline rendering copy receives multiplier support. The prepared
            // payload and existing kit files remain untouched, including their revisions.
            var content = strokeScale == 1 ? source.Content : StrokeWidthDeclaration().Replace(source.Content,
                "stroke-width:calc($1 * var(--icon-stroke-scale,1));");
            if (strokeScale != 1)
            {
                AddStyle(svg, "--icon-stroke-scale", strokeScale.ToString("R", CultureInfo.InvariantCulture));
            }

            // Only validated source drawing bypasses encoding. Caller attributes never do.
            if (transform.IsIdentity)
            {
                svg.InnerHtml.AppendHtml(content);
            }
            else
            {
                var group = new TagBuilder("g");
                group.Attributes["transform"] = transform.ToSvg(source.ViewBox);
                // Keep an original root transform inside the mapping transform, so the
                // mapping acts on the source result rather than changing its coordinate frame.
                if (svg.Attributes.TryGetValue("transform", out var originalTransform))
                {
                    svg.Attributes.Remove("transform");
                    var original = new TagBuilder("g");
                    original.Attributes["transform"] = originalTransform;
                    original.InnerHtml.AppendHtml(content);
                    group.InnerHtml.AppendHtml(original);
                }
                else
                {
                    group.InnerHtml.AppendHtml(content);
                }

                // Existing direct-child CSS sets transform-origin for generic utilities.
                // Nest the mapping group so its coordinate transform keeps SVG's native origin.
                var container = new TagBuilder("g");
                container.InnerHtml.AppendHtml(group);
                svg.InnerHtml.AppendHtml(container);
            }
        }

        if (options != null)
        {
            ApplyPresentation(svg, options);
            ApplyAttributes(svg, options);
        }

        svg.AddCssClass($"icon icon-{icon.LibraryKey} icon-{icon.LibraryKey}-{icon.VariantKey}");
        svg.Attributes["data-icon"] = icon.Address;
        svg.Attributes["xmlns"] = "http://www.w3.org/2000/svg";
        svg.Attributes["focusable"] = "false";

        // Preserve the existing accessible-name conventions for both output paths.
        var hasLabel = svg.Attributes.ContainsKey("aria-label") || svg.Attributes.ContainsKey("aria-labelledby");
        if (!svg.Attributes.ContainsKey("aria-hidden") && !hasLabel)
        {
            svg.Attributes["aria-hidden"] = "true";
        }

        if (hasLabel && !svg.Attributes.ContainsKey("role"))
        {
            svg.Attributes["role"] = "img";
        }

        return svg;
    }

    /// <inheritdoc />
    public TagBuilder RenderStack(IHtmlContent content, IconOptions options = null)
    {
        Guard.NotNull(content);

        var host = new TagBuilder("span");
        if (options != null)
        {
            ApplyPresentation(host, options);
            AddNumber(host, "--icon-rotate", options.Rotate, "deg");
            if (options.FlipHorizontal.HasValue)
            {
                AddNumber(host, "--icon-flip-x", options.FlipHorizontal.Value ? -1 : 1);
            }

            if (options.FlipVertical.HasValue)
            {
                AddNumber(host, "--icon-flip-y", options.FlipVertical.Value ? -1 : 1);
            }

            ApplyAttributes(host, options);
        }

        host.AddCssClass("icon-stack");
        if (host.Attributes.ContainsKey("aria-label") || host.Attributes.ContainsKey("aria-labelledby"))
        {
            host.Attributes.TryAdd("role", "img");
        }

        host.InnerHtml.AppendHtml(content);
        return host;
    }

    private static void ApplyAttributes(TagBuilder svg, IconOptions options)
    {
        foreach (var attribute in options.Attributes)
        {
            if (attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase))
            {
                svg.AddCssClass(attribute.Value);
            }
            else if (attribute.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                svg.Attributes.TryGetValue("style", out var sourceStyle);
                svg.Attributes["style"] = string.IsNullOrWhiteSpace(sourceStyle)
                    ? attribute.Value
                    : sourceStyle.TrimEnd(';') + "; " + attribute.Value;
            }
            else if (!attribute.Key.Equals("viewBox", StringComparison.OrdinalIgnoreCase)
                && !attribute.Key.Equals("width", StringComparison.OrdinalIgnoreCase)
                && !attribute.Key.Equals("height", StringComparison.OrdinalIgnoreCase))
            {
                svg.Attributes[attribute.Key] = attribute.Value;
            }
        }
    }

    // Match only stroke declarations emitted by IconSvgParser. No XML tree is reparsed
    // and heterogeneous source widths keep their individual prepared fallbacks.
    [GeneratedRegex(@"stroke-width:(var\(--icon-stroke-width,[^;]*\));", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex StrokeWidthDeclaration();

    private static void ApplyPresentation(TagBuilder svg, IconOptions options)
    {
        if (options.Size != null)
        {
            if (options.Size is not ("2xs" or "xs" or "sm" or "lg" or "xl" or "2xl"
                or "1x" or "2x" or "3x" or "4x" or "5x" or "6x" or "7x" or "8x" or "9x" or "10x"))
            {
                throw new ArgumentException("Unsupported icon size.", nameof(options));
            }

            svg.AddCssClass("icon-" + options.Size);
        }

        if (options.Animation != null)
        {
            if (options.Animation is not ("spin" or "pulse" or "spin-pulse" or "beat" or "fade" or "throb"
                or "cylon" or "cylon-vertical" or "bounce" or "beat-fade" or "flip" or "shake"))
            {
                throw new ArgumentException("Unsupported icon animation.", nameof(options));
            }

            svg.AddCssClass("icon-" + options.Animation);
        }

        if (options.FixedWidth)
        {
            svg.AddCssClass("icon-fw");
        }

        if (options.Inverse)
        {
            svg.AddCssClass("icon-inverse");
        }

        AddNumber(svg, "--icon-size-factor", options.FontScale);
        AddNumber(svg, "--icon-scale", options.Scale);
        AddNumber(svg, "--icon-shift-x", options.ShiftX / 16, "em");
        AddNumber(svg, "--icon-shift-y", options.ShiftY / 16, "em");
        AddStyle(svg, "--icon-color", options.Color);
        AddStyle(svg, "--icon-animation-duration", options.AnimationDuration);
        if (options.AnimationReverse.HasValue)
        {
            AddStyle(svg, "--icon-animation-direction", options.AnimationReverse.Value ? "reverse" : "normal");
        }
    }

    private static void AddNumber(TagBuilder svg, string property, float? value, string unit = null)
    {
        if (value.HasValue)
        {
            if (!float.IsFinite(value.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            AddStyle(svg, property, value.Value.ToString("R", CultureInfo.InvariantCulture) + unit);
        }
    }

    private static void AddStyle(TagBuilder svg, string property, string value)
    {
        if (value == null)
        {
            return;
        }

        if (value.IndexOfAny([';', '{', '}']) >= 0)
        {
            throw new ArgumentException("A presentation value must contain a single CSS value.", nameof(value));
        }

        svg.Attributes.TryGetValue("style", out var style);
        svg.Attributes["style"] = (string.IsNullOrWhiteSpace(style) ? string.Empty : style.TrimEnd(';') + "; ")
            + property + ":" + value + ";";
    }
}
