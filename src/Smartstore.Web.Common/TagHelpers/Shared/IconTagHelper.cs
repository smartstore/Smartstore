using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media.Icons;

namespace Smartstore.Web.TagHelpers.Shared;

/// <summary>
/// Binds shared icon presentation attributes without deciding how SVG content is rendered.
/// </summary>
public abstract class IconTagHelperBase : SmartTagHelper
{
    private const string SizeAttributeName = "size";
    private const string FontScaleAttributeName = "font-scale";
    private const string FixedWidthAttributeName = "fw";
    private const string ColorAttributeName = "color";
    private const string InverseAttributeName = "inverse";
    private const string AnimationAttributeName = "animation";
    private const string AnimationDurationAttributeName = "animation-duration";
    private const string AnimationReverseAttributeName = "animation-reverse";
    private const string RotateAttributeName = "rotate";
    private const string FlipHorizontalAttributeName = "flip-h";
    private const string FlipVerticalAttributeName = "flip-v";
    private const string ScaleAttributeName = "scale";
    private const string ShiftXAttributeName = "shift-x";
    private const string ShiftYAttributeName = "shift-y";
    private const string ClassAttributeName = "class";
    private const string StyleAttributeName = "style";

    private static readonly HashSet<string> _boundAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ClassAttributeName, StyleAttributeName,
        SizeAttributeName,
        FontScaleAttributeName,
        FixedWidthAttributeName,
        ColorAttributeName,
        InverseAttributeName,
        AnimationAttributeName,
        AnimationDurationAttributeName,
        AnimationReverseAttributeName,
        RotateAttributeName,
        FlipHorizontalAttributeName,
        FlipVerticalAttributeName,
        ScaleAttributeName,
        ShiftXAttributeName,
        ShiftYAttributeName,
    };

    /// <summary>
    /// Gets or sets a predefined size: 2xs, xs, sm, lg, xl, 2xl, or 1x through 10x.
    /// </summary>
    [HtmlAttributeName(SizeAttributeName)]
    public string Size { get; set; }

    /// <summary>
    /// Gets or sets a custom size multiplier taking precedence over Size.
    /// </summary>
    [HtmlAttributeName(FontScaleAttributeName)]
    public float? FontScale { get; set; }

    /// <summary>
    /// Gets or sets whether the icon occupies a fixed-width slot.
    /// </summary>
    [HtmlAttributeName(FixedWidthAttributeName)]
    public bool FixedWidth { get; set; }

    /// <summary>
    /// Gets or sets a CSS color. Omission preserves inherited color.
    /// </summary>
    [HtmlAttributeName(ColorAttributeName)]
    public string Color { get; set; }

    /// <summary>
    /// Gets or sets whether the inverse foreground color is used.
    /// </summary>
    [HtmlAttributeName(InverseAttributeName)]
    public bool Inverse { get; set; }

    /// <summary>
    /// Gets or sets an animation: spin, pulse, spin-pulse, beat, fade, throb, cylon,
    /// cylon-vertical, bounce, beat-fade, flip, or shake.
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
    public bool? AnimationReverse { get; set; }

    /// <summary>
    /// Gets or sets clockwise degrees, replacing the address or mapping rotation. Zero resets it.
    /// </summary>
    [HtmlAttributeName(RotateAttributeName)]
    public int? Rotate { get; set; }

    /// <summary>
    /// Gets or sets horizontal mirroring, replacing the address or mapping value.
    /// </summary>
    [HtmlAttributeName(FlipHorizontalAttributeName)]
    public bool? FlipHorizontal { get; set; }

    /// <summary>
    /// Gets or sets vertical mirroring, replacing the address or mapping value.
    /// </summary>
    [HtmlAttributeName(FlipVerticalAttributeName)]
    public bool? FlipVertical { get; set; }

    /// <summary>
    /// Gets or sets the drawing scale without changing its layout size.
    /// </summary>
    [HtmlAttributeName(ScaleAttributeName)]
    public float? Scale { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset in sixteenths of an em. Positive values move right.
    /// </summary>
    [HtmlAttributeName(ShiftXAttributeName)]
    public float? ShiftX { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset in sixteenths of an em. Positive values move down.
    /// </summary>
    [HtmlAttributeName(ShiftYAttributeName)]
    public float? ShiftY { get; set; }

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
    /// Transfers presentation and caller-owned DOM attributes to the shared renderer.
    /// </summary>
    /// <param name="output">The current output, including unbound attributes.</param>
    protected IconOptions CreateOptions(TagHelperOutput output)
    {
        var options = new IconOptions
        {
            Size = Size,
            FontScale = FontScale,
            FixedWidth = FixedWidth,
            Color = Color,
            Inverse = Inverse,
            Animation = Animation,
            AnimationDuration = AnimationDuration,
            AnimationReverse = AnimationReverse,
            Rotate = Rotate,
            FlipHorizontal = FlipHorizontal,
            FlipVertical = FlipVertical,
            Scale = Scale,
            ShiftX = ShiftX,
            ShiftY = ShiftY,
        };
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
/// <param name="iconService">Resolves icon addresses and supplies validated, cached SVG payloads.</param>
/// <param name="iconRenderer">Owns complete SVG rendering, including automatic kit references.</param>
[HtmlTargetElement("icon", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
[OutputElementHint("svg")]
public class IconTagHelper(IIconService iconService, IIconRenderer iconRenderer) : IconTagHelperBase
{
    const string NameAttributeName = "name";
    const string LibraryAttributeName = "lib";
    const string VariantAttributeName = "variant";

    private const string StrokeScaleAttributeName = "stroke-scale";

    /// <summary>
    /// Gets or sets the icon or conceptual name, optionally qualified as library:name@variant.
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
        Guard.NotEmpty(Name);

        var cancelToken = ViewContext.HttpContext.RequestAborted;
        var icon = await iconService.GetIconAsync(Name, Library, Variant, cancelToken);
        if (icon == null)
        {
            output.SuppressOutput();
            return;
        }

        var options = CreateOptions(output);
        options.StrokeScale = StrokeScale;
        options.Attributes.Remove(NameAttributeName);
        options.Attributes.Remove(LibraryAttributeName);
        options.Attributes.Remove(VariantAttributeName);
        options.Attributes.Remove(StrokeScaleAttributeName);

        var svg = await iconRenderer.RenderAsync(icon, options, cancelToken);
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
        ApplyElement(output, iconRenderer.RenderStack(content, CreateOptions(output)));
    }
}
