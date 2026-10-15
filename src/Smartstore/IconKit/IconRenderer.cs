using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.IconKit;

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
        var area = options?.Area ?? icon?.Area;
        var requestedName = options?.RequestedName.NullEmpty() ?? icon?.ConceptName.NullEmpty() ?? icon?.Address.NullEmpty() ?? icon?.Name;
        var missing = icon == null;
        if (missing)
        {
            icon = await icons.GetIconAsync("system:not-found!/default", area: area, cancelToken: cancelToken);
            if (icon == null)
            {
                return null;
            }
        }

        cancelToken.ThrowIfCancellationRequested();
        var transform = new IconTransform(options?.FlipHorizontal ?? icon.Transform.FlipX,
            options?.FlipVertical ?? icon.Transform.FlipY, options?.Rotate ?? icon.Transform.Rotation);
        var strokeScale = icon.SupportsStrokeScaling ? options?.StrokeScale ?? icon.StrokeScale : 1;
        if (!double.IsFinite(strokeScale) || strokeScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Stroke scale must be finite and positive.");
        }

        // Kit symbols already contain mapping transforms. Replacing one requires the
        // original source, not another transform around the existing symbol.
        var reference = options?.ForceInline == true || icon.RequiresInline || transform != icon.Transform || strokeScale != icon.StrokeScale
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
                // Never recurse if the system placeholder itself is unavailable.
                if (missing) return null;
                var fallbackOptions = options?.Clone() ?? new IconOptions();
                fallbackOptions.Area = area;
                fallbackOptions.RequestedName = requestedName;
                return await RenderAsync(null, fallbackOptions, cancelToken);
            }

            foreach (var attribute in source.RootAttributes)
            {
                svg.Attributes[attribute.Key] = attribute.Value;
            }
            svg.Attributes["viewBox"] = source.ViewBox;
            // Only the inline rendering copy receives multiplier support. The prepared
            // payload and existing kit files remain untouched, including their revisions.
            var content = strokeScale == 1 ? source.Content : IconSvgParser.ScaleStrokeWidths(source.Content, "var(--icon-stroke-scale,1)");
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

        ApplySizeAndAnimation(svg, options?.Size ?? icon.Size, options?.Animation ?? icon.Animation);
        if (options?.Color == null)
        {
            AddStyle(svg, "--icon-color", icon.Color);
        }

        if (options != null)
        {
            ApplyPresentation(svg, options);
            ApplyAttributes(svg, options);
        }

        svg.AddCssClass($"icon icon-{icon.LibraryKey} icon-{icon.LibraryKey}-{icon.VariantKey}");
        if (icon.IsDuotone)
        {
            svg.AddCssClass("icon-duotone");
        }

        if (options?.MirrorRtl ?? icon.MirrorInRtl)
        {
            svg.AddCssClass("icon-mirror-rtl");
        }

        if (missing)
        {
            svg.AddCssClass("icon-not-found");
        }

        svg.Attributes["data-supports-stroke-scaling"] = icon.SupportsStrokeScaling ? "true" : "false";
        svg.Attributes["data-icon"] = missing && requestedName.HasValue()
            ? $"{requestedName} -> {icon.Address}"
            : icon.ConceptName == null ? icon.Address : $"{icon.ConceptName} -> {icon.Address}";
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
            ApplySizeAndAnimation(host, options.Size, options.Animation);
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
        if (options?.MirrorRtl == true)
        {
            host.AddCssClass("icon-mirror-rtl");
        }
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

    private static void ApplySizeAndAnimation(TagBuilder svg, string size, string animation)
    {
        if (size != null)
        {
            if (!IconModifiers.IsValidSize(size))
            {
                throw new ArgumentException("Unsupported icon size.", nameof(size));
            }

            svg.AddCssClass("icon-" + size);
        }

        if (animation != null)
        {
            if (!IconModifiers.IsValidAnimation(animation))
            {
                throw new ArgumentException("Unsupported icon animation.", nameof(animation));
            }

            svg.AddCssClass("icon-" + animation);
        }
    }

    private static void ApplyPresentation(TagBuilder svg, IconOptions options)
    {
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
