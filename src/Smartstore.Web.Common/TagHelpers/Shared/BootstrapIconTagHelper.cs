using Humanizer;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    public enum IconAnimation
    {
        Fade,
        Spin,
        SpinReverse,
        SpinPulse,
        SpinPulseReverse,
        Beat,
        Throb,
        Cylon,
        CylonVertical
    }

    [HtmlTargetElement("bootstrap-iconstack"), RestrictChildren("bootstrap-icon")]
    public class BootstrapIconStackTagHelper : TagHelper
    {
        const string ClassAttributeName = "class";
        const string StyleAttributeName = "style";
        const string IdAttributeName = "id";
        const string FontScaleAttributeName = "font-scale";
        const string FillAttributeName = "fill";
        const string AnimationAttributeName = "animation";

        const string ShiftXAttributeName = "shift-x";
        const string ShiftYAttributeName = "shift-y";
        const string FlipHAttributeName = "flip-h";
        const string FlipVAttributeName = "flip-v";
        const string RotateAttributeName = "rotate";
        const string ScaleAttributeName = "scale";

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// CSS class.
        /// </summary>
        [HtmlAttributeName(ClassAttributeName)]
        public string CssClass { get; set; }

        /// <summary>
        /// CSS style.
        /// </summary>
        [HtmlAttributeName(StyleAttributeName)]
        public string Style { get; set; }

        /// <summary>
        /// HTML id.
        /// </summary>
        [HtmlAttributeName(IdAttributeName)]
        public string Id { get; set; }

        /// <summary>
        /// Scales the icons current font size. e.g.: 2.5 --> 250% font-size.
        /// </summary>
        [HtmlAttributeName(FontScaleAttributeName)]
        public float? FontScale { get; set; }

        /// <summary>
        /// Icon color. Default: currentColor (inherits CSS <c>color</c>).
        /// </summary>
        [HtmlAttributeName(FillAttributeName)]
        public string Fill { get; set; } = "currentColor";

        /// <summary>
        /// Animates the icon.
        /// </summary>
        [HtmlAttributeName(AnimationAttributeName)]
        public IconAnimation? Animation { get; set; }

        /// <summary>
        /// Moves the icon horizontally. Positive numbers will shift the icon right, negative left. Value is in 1/16em units. Default: 0.
        /// </summary>
        [HtmlAttributeName(ShiftXAttributeName)]
        public int ShiftX { get; set; }

        /// <summary>
        /// Moves the icon vertically. Positive numbers will shift the icon down, negative up. Value is in 1/16em units. Default: 0.
        /// </summary>
        [HtmlAttributeName(ShiftYAttributeName)]
        public int ShiftY { get; set; }

        /// <summary>
        /// Flips the icon horizontally. Default: false.
        /// </summary>
        [HtmlAttributeName(FlipHAttributeName)]
        public bool FlipH { get; set; }

        /// <summary>
        /// Flips the icon vertically. Default: false.
        /// </summary>
        [HtmlAttributeName(FlipVAttributeName)]
        public bool FlipV { get; set; }

        /// <summary>
        /// Rotates the icon by the specified number of degrees. Positive values rotate clockwise, while negative values rotate counterclockwise. Default: 0.
        /// </summary>
        [HtmlAttributeName(RotateAttributeName)]
        public int Rotate { get; set; }

        /// <summary>
        /// Scales the icon's SVG, without increasing the font size. Default: 1.
        /// </summary>
        [HtmlAttributeName(ScaleAttributeName)]
        public float Scale { get; set; } = 1;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            context.Items["IconStack"] = true;
            
            output.TagName = "svg";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.AppendCssClass("bi bi-stack");

            var animClass = GetAnimationClassName();
            if (animClass != null)
            {
                output.AppendCssClass(animClass);
            }

            ApplyRootAttributes(output, false);

            var g = ApplyTransforms(new TagBuilder("g"));
            output.WrapContentWith(g);
        }

        protected string GetAnimationClassName()
        {
            if (Animation.HasValue)
            {
                return $"animate-{Animation.Value.ToString().Kebaberize()}";
            }

            return null;
        }

        protected void ApplyRootAttributes(TagHelperOutput output, bool isStackItem)
        {
            if (Id.HasValue())
            {
                output.MergeAttribute("id", Id);
            }

            if (CssClass.HasValue())
            {
                output.AppendCssClass(CssClass);
            }

            if (Style.HasValue())
            {
                output.MergeAttribute("style", Style);
            }

            if (Fill.HasValue())
            {
                output.MergeAttribute("fill", Fill);
            }

            if (!isStackItem)
            {
                if (FontScale > 0)
                {
                    output.AddCssStyle("font-size", $"{FontScale.Value * 100}%");
                }

                output.MergeAttribute("width", "1em");
                output.MergeAttribute("height", "1em");
                output.MergeAttribute("role", "img");
                output.MergeAttribute("focusable", "false");
            }
        }

        protected TagBuilder ApplyTransforms(TagBuilder g)
        {
            // Compute the transforms:
            // Note that order is important as SVG transforms are applied in order from
            // left to right and we want flipping/scale to occur before rotation.
            // Note: shifting is applied separately
            // Assumes that the viewbox is "0 0 16 16" ("8 8" is the center)

            var hasScale = FlipH || FlipV || Scale != 1;
            var hasTransforms = hasScale || Rotate != 0;
            var hasShift = ShiftX != 0 || ShiftY != 0;

            if (hasTransforms || hasShift)
            {
                var transforms = new List<string>();

                //transforms.Add("translate(8 8)");

                if (hasScale)
                {
                    var scaleH = ((FlipH ? -1 : 1) * Scale).ToStringInvariant();
                    var scaleV = ((FlipV ? -1 : 1) * Scale).ToStringInvariant();
                    transforms.Add($"scale({scaleH} {scaleV})");
                }

                if (hasShift)
                {
                    transforms.Add($"translate({ShiftX * 16} {ShiftY * 16})");
                }

                if (Rotate != 0)
                {
                    transforms.Add($"rotate({Rotate})");
                }

                //transforms.Add("translate(-8 -8)");

                if (transforms.Count > 0)
                {
                    g.Attributes["transform"] = string.Join(' ', transforms);
                }
            }

            return g;
        }
    }

    [HtmlTargetElement("bootstrap-icon", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class BootstrapIconTagHelper : BootstrapIconStackTagHelper
    {
        const string NameAttributeName = "name";

        /// <summary>
        /// The name of the Bootstrap icon to use.
        /// </summary>
        [HtmlAttributeName(NameAttributeName)]
        public string Name { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var isStackItem = context.Items.ContainsKey("IconStack");
            
            output.TagName = "svg";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.AppendCssClass("bi");

            ApplyRootAttributes(output, isStackItem);

            var urlHelper = ViewContext.HttpContext.RequestServices.GetService<IUrlHelper>();
            var symbol = new TagBuilder("use");
            symbol.Attributes["xlink:href"] = urlHelper.Content($"~/lib/bi/bootstrap-icons.svg#{Name}");

            var el = symbol;

            var animClass = GetAnimationClassName();
            if (animClass != null)
            {
                if (isStackItem)
                {
                    el.AppendCssClass(animClass);
                }
                else
                {
                    output.AppendCssClass(animClass);
                }
            }

            if (isStackItem)
            {
                el = new TagBuilder("g");
                el.InnerHtml.AppendHtml(symbol);
            }

            ApplyTransforms(el);

            output.Content.AppendHtml(el);
        }
    }
}
