using Humanizer;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("bootstrap-iconstack"), RestrictChildren("bootstrap-icon")]
    public class BootstrapIconStackTagHelper : SmartTagHelper
    {
        const string ClassAttributeName = "class";
        const string StyleAttributeName = "style";
        const string FontScaleAttributeName = "font-scale";
        const string FillAttributeName = "fill";
        const string AnimationAttributeName = "animation";

        const string ShiftXAttributeName = "shift-x";
        const string ShiftYAttributeName = "shift-y";
        const string FlipHAttributeName = "flip-h";
        const string FlipVAttributeName = "flip-v";
        const string RotateAttributeName = "rotate";
        const string ScaleAttributeName = "scale";

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
        public CssAnimation? Animation { get; set; }

        /// <summary>
        /// Moves the icon horizontally. Positive numbers will shift the icon right, negative left. Value is in 1/16em units. Default: 0.
        /// </summary>
        [HtmlAttributeName(ShiftXAttributeName)]
        public float ShiftX { get; set; }

        /// <summary>
        /// Moves the icon vertically. Positive numbers will shift the icon down, negative up. Value is in 1/16em units. Default: 0.
        /// </summary>
        [HtmlAttributeName(ShiftYAttributeName)]
        public float ShiftY { get; set; }

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

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            context.Items["IconStack"] = true;

            output.TagName = "svg";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.AppendCssClass("bi bi-stack");

            var animClass = GenerateAnimationClassName();
            if (animClass != null)
            {
                output.AppendCssClass(animClass);
            }

            ApplyRootAttributes(output, false);

            var g = new TagBuilder("g");
            var transforms = GenerateTransforms();

            if (transforms != null)
            {
                g.Attributes["transform"] = transforms;
            }

            output.WrapContentWith(g);
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;

        protected string GenerateAnimationClassName()
        {
            if (Animation.HasValue)
            {
                return $"animate-{Animation.Value.ToString().Kebaberize()}";
            }

            return null;
        }

        protected void ApplyRootAttributes(TagHelperOutput output, bool isStackItem)
        {
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

        protected object GenerateHtmlAttributes()
        {
            var attrs = new Dictionary<string, object>();

            if (CssClass.HasValue())
            {
                attrs["class"] = CssClass;
            }

            if (Style.HasValue())
            {
                attrs["style"] = Style;
            }

            return attrs;
        }

        protected string GenerateTransforms()
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
                    return string.Join(' ', transforms);
                }
            }

            return null;
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

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            var isStackItem = context.Items.ContainsKey("IconStack");

            var svg = (TagBuilder)HtmlHelper.BootstrapIcon(
                Name,
                isStackItem,
                Fill,
                FontScale,
                Animation,
                GenerateTransforms(),
                GenerateHtmlAttributes());

            output.TagName = svg.TagName;
            output.TagMode = TagMode.StartTagAndEndTag;

            output.MergeAttributes(svg);
            output.Content.AppendHtml(svg.InnerHtml);
        }
    }
}
