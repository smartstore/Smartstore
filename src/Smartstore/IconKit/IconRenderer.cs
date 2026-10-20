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
    public async Task<TagBuilder> RenderAsync(IconInfo icon, IconPresentation presentation = default, IconOptions options = null, CancellationToken cancelToken = default)
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
        var transform = icon.Transform;
        var strokeScale = icon.SupportsStrokeScaling ? presentation.StrokeScale ?? icon.StrokeScale : 1;
        if (!double.IsFinite(strokeScale) || strokeScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(presentation), "Stroke scale must be finite and positive.");
        }

        // Presentation overrides retain the original kit symbol.
        var reference = options?.ForceInline == true
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
                return await RenderAsync(null, presentation, fallbackOptions, cancelToken);
            }

            foreach (var attribute in source.RootAttributes)
            {
                svg.Attributes[attribute.Key] = attribute.Value;
            }
            svg.Attributes["viewBox"] = source.ViewBox;
            // Inherited CSS overrides the mapping default without rebuilding the drawing.
            var multiplier = "var(--icon-stroke-scale,var(--_icon-stroke-scale," + icon.StrokeScale.ToString("R", CultureInfo.InvariantCulture) + "))";
            var content = icon.SupportsStrokeScaling ? IconSvgParser.ScaleStrokeWidths(source.Content, multiplier) : source.Content;
            if (icon.SupportsStrokeScaling && svg.Attributes.TryGetValue("style", out var rootStyle))
            {
                svg.Attributes["style"] = IconSvgParser.ScaleStrokeWidths(rootStyle, multiplier);
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

        ApplySizeAndAnimation(svg, presentation.Size, presentation.Animation);

        ApplyPresentation(svg, presentation);
        // Cancel only the baked mapping components overridden by presentation.
        // CSS applies rotation before scale, so a single-axis flip reverses the baked rotation.
        var flipX = presentation.FlipHorizontal.HasValue && presentation.FlipHorizontal.Value != transform.FlipX;
        var flipY = presentation.FlipVertical.HasValue && presentation.FlipVertical.Value != transform.FlipY;
        ApplyFlip(svg, presentation.FlipHorizontal.HasValue ? flipX : null,
            presentation.FlipVertical.HasValue ? flipY : null);
        if (presentation.Rotate.HasValue || flipX != flipY)
        {
            ApplyRotation(svg, (presentation.Rotate ?? transform.Rotation)
                - (flipX != flipY ? -transform.Rotation : transform.Rotation));
        }
        if (icon.SupportsStrokeScaling) AddNumber(svg, "--icon-stroke-scale", presentation.StrokeScale);
        if (!transform.IsIdentity)
        {
            svg.Attributes["data-icon-mapping"] = FormattableString.Invariant($"{(transform.FlipX ? 1 : 0)} {(transform.FlipY ? 1 : 0)} {transform.Rotation:R}");
        }
        if (options != null)
        {
            ApplyAttributes(svg, options);
        }

        svg.AddCssClass($"icon icon-{icon.LibraryKey} icon-{icon.LibraryKey}-{icon.VariantKey}");
        if (icon.IsDuotone)
        {
            svg.AddCssClass("icon-duotone");
        }

        if (presentation.MirrorRtl ?? icon.MirrorInRtl)
        {
            svg.AddCssClass("icon-mirror-rtl");
        }

        if (missing)
        {
            svg.AddCssClass("icon-not-found");
        }

        if (icon.SupportsStrokeScaling)
        {
            svg.Attributes["data-supports-stroke-scaling"] = "true";
        }
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
    public async Task<TagBuilder> RenderAsync(IconExpression expression, IconOptions options = null, CancellationToken cancelToken = default)
    {
        var icon = await icons.GetIconAsync(expression.Address, area: options?.Area, cancelToken: cancelToken);
        if (icon == null && options?.RequestedName == null)
        {
            options = options?.Clone() ?? new IconOptions();
            options.RequestedName = expression.ToString();
        }

        return await RenderAsync(icon, expression.Presentation, options, cancelToken);
    }

    /// <inheritdoc />
    public TagBuilder RenderStack(IHtmlContent content, IconPresentation presentation = default, IconOptions options = null)
    {
        Guard.NotNull(content);

        var host = new TagBuilder("span");
        ApplySizeAndAnimation(host, presentation.Size, presentation.Animation);
        ApplyPresentation(host, presentation);
        ApplyRotation(host, presentation.Rotate);
        ApplyFlip(host, presentation.FlipHorizontal, presentation.FlipVertical);

        if (options != null)
        {
            ApplyAttributes(host, options);
        }

        host.AddCssClass("icon-stack");
        if (presentation.MirrorRtl == true)
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
            if (!IconPresentation.IsValidSize(size))
            {
                throw new ArgumentException("Unsupported icon size.", nameof(size));
            }

            svg.AddCssClass("icon-" + size);
        }

        if (animation != null)
        {
            if (!IconPresentation.IsValidAnimation(animation))
            {
                throw new ArgumentException("Unsupported icon animation.", nameof(animation));
            }

            svg.AddCssClass("icon-" + animation);
        }
    }

    private static void ApplyPresentation(TagBuilder svg, IconPresentation presentation)
    {
        if (presentation.FixedWidth == true)
        {
            svg.AddCssClass("icon-fw");
        }

        if (presentation.Inverse == true)
        {
            svg.AddCssClass("icon-inverse");
        }

        AddNumber(svg, "--icon-size-factor", presentation.FontScale);
        AddNumber(svg, "--icon-scale", presentation.Scale);
        AddNumber(svg, "--icon-shift-x", presentation.ShiftX / 16, "em");
        AddNumber(svg, "--icon-shift-y", presentation.ShiftY / 16, "em");
        AddStyle(svg, "--icon-color", presentation.Color);
        AddNumber(svg, "--icon-opacity", presentation.Opacity);
        AddStyle(svg, "--icon-animation-duration", presentation.AnimationDuration);
        if (presentation.AnimationReverse.HasValue)
        {
            AddStyle(svg, "--icon-animation-direction", presentation.AnimationReverse.Value ? "reverse" : "normal");
        }
    }

    private static void ApplyFlip(TagBuilder host, bool? horizontal, bool? vertical)
    {
        if (horizontal == true && vertical == true)
        {
            host.AddCssClass("icon-flip-hv");
        }
        else
        {
            if (horizontal.HasValue) host.AddCssClass(horizontal.Value ? "icon-flip-h" : "icon-flip-h-reset");
            if (vertical.HasValue) host.AddCssClass(vertical.Value ? "icon-flip-v" : "icon-flip-v-reset");
        }
    }

    private static void ApplyRotation(TagBuilder host, double? angle)
    {
        if (!angle.HasValue) return;

        var normalized = (angle.Value % 360 + 360) % 360;
        if (normalized is 90 or 180 or 270)
        {
            host.AddCssClass("icon-rotate-" + normalized.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            AddNumber(host, "--icon-rotate", angle, "deg");
        }
    }

    private static void AddNumber(TagBuilder svg, string property, double? value, string unit = null)
    {
        if (value.HasValue)
        {
            if (!double.IsFinite(value.Value))
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
