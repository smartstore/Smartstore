using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.IconKit;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared;

/// <summary>
/// Binds shared icon presentation attributes without deciding how SVG content is rendered.
/// </summary>
public abstract class IconTagHelperBase : SmartTagHelper
{
    private bool? _fixedWidth;
    private bool? _inverse;
    private bool? _animationReverse;
    private bool? _flipHorizontal;
    private bool? _flipVertical;
    private bool? _mirrorRtl;

    private const string PositionAttributeName = "position";
    private const string SizeAttributeName = "size";
    private const string FontScaleAttributeName = "font-scale";
    private const string FixedWidthAttributeName = "fw";
    private const string ColorAttributeName = "color";
    private const string OpacityAttributeName = "opacity";
    private const string InverseAttributeName = "inverse";
    private const string AnimationAttributeName = "animation";
    private const string AnimationDurationAttributeName = "animation-duration";
    private const string AnimationReverseAttributeName = "animation-reverse";
    private const string RotateAttributeName = "rotate";
    private const string FlipHorizontalAttributeName = "flip-h";
    private const string FlipVerticalAttributeName = "flip-v";
    private const string MirrorRtlAttributeName = "mirror-rtl";
    private const string ScaleAttributeName = "scale";
    private const string ShiftXAttributeName = "shift-x";
    private const string ShiftYAttributeName = "shift-y";
    private const string ClassAttributeName = "class";
    private const string StyleAttributeName = "style";

    private static readonly HashSet<string> _boundAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ClassAttributeName, StyleAttributeName,
        SizeAttributeName,
        PositionAttributeName,
        FontScaleAttributeName,
        FixedWidthAttributeName,
        ColorAttributeName,
        OpacityAttributeName,
        InverseAttributeName,
        AnimationAttributeName,
        AnimationDurationAttributeName,
        AnimationReverseAttributeName,
        RotateAttributeName,
        FlipHorizontalAttributeName,
        FlipVerticalAttributeName,
        MirrorRtlAttributeName,
        ScaleAttributeName,
        ShiftXAttributeName,
        ShiftYAttributeName,
    };

    /// <summary>
    /// Gets or sets the position within a stack: start-start, start-center, start-end,
    /// center-start, center-end, end-start, end-center, or end-end.
    /// The block axis comes first, followed by the inline axis. Omission centers the
    /// icon within the stack area. Has no positioning effect outside a stack.
    /// </summary>
    [HtmlAttributeName(PositionAttributeName)]
    public string Position { get; set; }

    /// <summary>
    /// Gets or sets a predefined size: 2xs, xs, sm, lg, xl, 2xl, or 1x through 10x.
    /// </summary>
    [HtmlAttributeName(SizeAttributeName)]
    public string Size { get; set; }

    /// <summary>
    /// Gets or sets a custom size multiplier taking precedence over Size.
    /// </summary>
    [HtmlAttributeName(FontScaleAttributeName)]
    public double? FontScale { get; set; }

    /// <summary>
    /// Gets or sets whether the icon occupies a fixed-width slot.
    /// </summary>
    [HtmlAttributeName(FixedWidthAttributeName)]
    public bool FixedWidth
    {
        get => _fixedWidth.GetValueOrDefault();
        set => _fixedWidth = value;
    }

    /// <summary>
    /// Gets or sets a CSS color. Omission preserves inherited color.
    /// </summary>
    [HtmlAttributeName(ColorAttributeName)]
    public string Color { get; set; }

    /// <summary>
    /// Gets or sets opacity from zero to one for the whole icon or stack.
    /// Overrides the expression modifier o; invalid values are ignored.
    /// </summary>
    [HtmlAttributeName(OpacityAttributeName)]
    public double? Opacity { get; set; }

    /// <summary>
    /// Gets or sets whether the inverse foreground color is used.
    /// </summary>
    [HtmlAttributeName(InverseAttributeName)]
    public bool Inverse
    {
        get => _inverse.GetValueOrDefault();
        set => _inverse = value;
    }

    /// <summary>
    /// Gets or sets an animation: spin, pulse, beat, fade, throb, bounce, flip, shake,
    /// spin-snap, swing, or wag.
    /// </summary>
    [HtmlAttributeName(AnimationAttributeName)]
    public string Animation { get; set; }

    /// <summary>
    /// Gets or sets a CSS animation duration, such as 800ms or 2s.
    /// </summary>
    [HtmlAttributeName(AnimationDurationAttributeName)]
    public string AnimationDuration { get; set; }

    /// <summary>
    /// Gets or sets reverse playback. Omission preserves the animation default.
    /// </summary>
    [HtmlAttributeName(AnimationReverseAttributeName)]
    public bool AnimationReverse
    {
        get => _animationReverse.GetValueOrDefault();
        set => _animationReverse = value;
    }

    /// <summary>
    /// Gets or sets clockwise degrees, replacing the address or mapping rotation. Zero resets it.
    /// </summary>
    [HtmlAttributeName(RotateAttributeName)]
    public int? Rotate { get; set; }

    /// <summary>
    /// Gets or sets horizontal mirroring, replacing the address or mapping value.
    /// </summary>
    [HtmlAttributeName(FlipHorizontalAttributeName)]
    public bool FlipHorizontal
    {
        get => _flipHorizontal.GetValueOrDefault();
        set => _flipHorizontal = value;
    }

    /// <summary>
    /// Gets or sets vertical mirroring, replacing the address or mapping value.
    /// </summary>
    [HtmlAttributeName(FlipVerticalAttributeName)]
    public bool FlipVertical
    {
        get => _flipVertical.GetValueOrDefault();
        set => _flipVertical = value;
    }

    /// <summary>
    /// Gets or sets whether to mirror horizontally in RTL. Null preserves the concept policy.
    /// </summary>
    [HtmlAttributeName(MirrorRtlAttributeName)]
    public bool MirrorRtl
    {
        get => _mirrorRtl.GetValueOrDefault();
        set => _mirrorRtl = value;
    }

    /// <summary>
    /// Gets or sets the drawing scale without changing its layout size.
    /// </summary>
    [HtmlAttributeName(ScaleAttributeName)]
    public double? Scale { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset in sixteenths of an em. Positive values move right.
    /// </summary>
    [HtmlAttributeName(ShiftXAttributeName)]
    public double? ShiftX { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset in sixteenths of an em. Positive values move down.
    /// </summary>
    [HtmlAttributeName(ShiftYAttributeName)]
    public double? ShiftY { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes.
    /// </summary>
    [HtmlAttributeName(ClassAttributeName)]
    public string CssClass { get; set; }

    /// <summary>
    /// Gets or sets CSS declarations taking precedence over generated presentation styles.
    /// </summary>
    [HtmlAttributeName(StyleAttributeName)]
    public string Style { get; set; }

    /// <inheritdoc />
    protected override string GenerateTagId(TagHelperContext context) => null;

    /// <summary>
    /// Collects explicit presentation values for an icon or stack.
    /// </summary>
    protected IconPresentation CreatePresentation() => new()
    {
        Size = Size,
        FontScale = FontScale,
        FixedWidth = _fixedWidth,
        Color = Color,
        Opacity = Opacity is >= 0 and <= 1 ? Opacity : null,
        Inverse = _inverse,
        Animation = Animation,
        AnimationDuration = AnimationDuration,
        AnimationReverse = _animationReverse,
        Rotate = Rotate,
        FlipHorizontal = _flipHorizontal,
        FlipVertical = _flipVertical,
        MirrorRtl = _mirrorRtl,
        Scale = Scale,
        ShiftX = ShiftX,
        ShiftY = ShiftY
    };

    /// <summary>
    /// Transfers caller-owned DOM attributes to the shared renderer.
    /// </summary>
    /// <param name="output">The current output, including unbound attributes.</param>
    protected IconOptions CreateOptions(TagHelperOutput output)
    {
        var options = new IconOptions();
        foreach (var attribute in output.Attributes)
        {
            if (!_boundAttributes.Contains(attribute.Name))
            {
                options.Attributes[attribute.Name] = attribute.ValueAsString();
            }
        }

        // Bound attributes are absent from normal Razor output. Read the output fallback
        // as well for helpers invoked directly, and pass each value exactly once.
        var cssClass = CssClass ?? output.Attributes[ClassAttributeName]?.ValueAsString();
        var style = Style ?? output.Attributes[StyleAttributeName]?.ValueAsString();
        if (!string.IsNullOrEmpty(Position))
        {
            cssClass = string.IsNullOrEmpty(cssClass) ? "icon-pos-" + Position : cssClass + " icon-pos-" + Position;
        }
        if (cssClass != null)
        {
            options.Attributes[ClassAttributeName] = cssClass;
        }

        if (style != null)
        {
            options.Attributes[StyleAttributeName] = style;
        }

        return options;
    }

    /// <summary>
    /// Transfers a complete rendered element to the Razor output.
    /// </summary>
    /// <param name="output">The destination output.</param>
    /// <param name="element">The complete element supplied by the renderer.</param>
    protected static void ApplyElement(TagHelperOutput output, TagBuilder element)
    {
        output.TagName = element.TagName;
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.Clear();
        foreach (var attribute in element.Attributes)
        {
            output.Attributes.SetAttribute(attribute.Key, attribute.Value);
        }

        output.Content.SetHtmlContent(element.InnerHtml);
    }
}

/// <summary>
/// Renders a local IconKit icon through the shared renderer. Presentation is controlled by icon CSS classes and variables.
/// </summary>
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
[OutputElementHint("svg")]
public class IconTagHelper : IconTagHelperBase
{
    /// <summary>
    /// Gets or sets the icon area. Omission derives it from the current MVC area.
    /// </summary>
    [HtmlAttributeName("area")]
    public string Area { get; set; }

    /// <summary>
    /// Gets or sets whether to embed the drawing instead of referencing a kit sprite.
    /// </summary>
    [HtmlAttributeName("force-inline")]
    public bool ForceInline { get; set; }

    private const string ExpressionAttributeName = "expression";
    const string NameAttributeName = "name";
    const string LibraryAttributeName = "lib";
    const string VariantAttributeName = "variant";

    private const string StrokeScaleAttributeName = "stroke-scale";

    /// <summary>
    /// Gets or sets a complete icon expression instead of name, library and variant.
    /// Explicit presentation attributes override its presentation values.
    /// </summary>
    [HtmlAttributeName(ExpressionAttributeName)]
    public IconExpression? Expression { get; set; }

    /// <summary>
    /// Gets or sets the icon or conceptual name, optionally qualified as library:name/variant.
    /// Supports flip, rotate and stroke-scale query modifiers; explicit helper properties override them.
    /// </summary>
    [HtmlAttributeName(NameAttributeName)]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the library system name or short name. Omission uses the address or configured default.
    /// </summary>
    [HtmlAttributeName(LibraryAttributeName)]
    public string Library { get; set; }

    /// <summary>
    /// Gets or sets the variant name or short name. Omission uses the address or effective library default.
    /// </summary>
    [HtmlAttributeName(VariantAttributeName)]
    public string Variant { get; set; }

    /// <summary>
    /// Gets or sets a positive stroke multiplier, replacing the address or mapping multiplier.
    /// </summary>
    [HtmlAttributeName(StrokeScaleAttributeName)]
    public double? StrokeScale { get; set; }

    /// <inheritdoc />
    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        IconExpression parsed;
        if (Expression.HasValue)
        {
            if (Name.HasValue() || Library.HasValue() || Variant.HasValue())
            {
                throw new ArgumentException("Expression cannot be combined with name, lib or variant.", nameof(Expression));
            }
            parsed = Expression.Value;
            if (parsed.Address.IsEmpty)
            {
                throw new ArgumentException("Expression must contain an icon address.", nameof(Expression));
            }
        }
        else
        {
            Guard.NotEmpty(Name);
            parsed = IconExpression.Parse(Name);
            var address = parsed.Address;
            parsed = new IconExpression(
                new IconAddress(address.Name, address.Library ?? Library.NullEmpty(), address.Variant ?? Variant.NullEmpty(), address.SkipMapping),
                parsed.Presentation);
        }

        // Razor binds null string attribute values as empty strings. Treat optional
        // selectors as omitted so configured defaults and qualified names still apply.
        var options = CreateOptions(output);
        options.Area = Area.NullEmpty();
        options.ForceInline = ForceInline;
        options.Attributes.Remove("force-inline");
        options.Attributes.Remove("area");
        options.Attributes.Remove(ExpressionAttributeName);
        options.Attributes.Remove(NameAttributeName);
        options.Attributes.Remove(LibraryAttributeName);
        options.Attributes.Remove(VariantAttributeName);
        options.Attributes.Remove(StrokeScaleAttributeName);

        var presentation = (CreatePresentation() with { StrokeScale = StrokeScale }).WithDefaults(parsed.Presentation);
        var expression = parsed with { Presentation = presentation };
        var svg = await HtmlHelper.IconAsync(expression, options) as TagBuilder;
        if (svg == null)
        {
            output.SuppressOutput();
            return;
        }

        ApplyElement(output, svg);
    }
}

/// <summary>
/// Hosts independent icon layers, each retaining its own SVG coordinate system.
/// </summary>
/// <param name="iconRenderer">Creates the stack host and applies shared presentation options.</param>
[HtmlTargetElement("icon-stack", TagStructure = TagStructure.NormalOrSelfClosing)]
[RestrictChildren("icon")]
[OutputElementHint("span")]
public class IconStackTagHelper(IIconRenderer iconRenderer) : IconTagHelperBase
{
    /// <inheritdoc />
    protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Render children normally; they independently choose inline SVG or kit references.
        var content = await output.GetChildContentAsync();
        ApplyElement(output, iconRenderer.RenderStack(content, CreatePresentation(), CreateOptions(output)));
    }
}
