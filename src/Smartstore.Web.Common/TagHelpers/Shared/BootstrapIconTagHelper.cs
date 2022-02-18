using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("bootstrap-icon", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class BootstrapIconTagHelper : TagHelper
    {
        const string NameAttributeName = "name";
        const string ClassAttributeName = "class";
        const string StyleAttributeName = "style";
        const string IdAttributeName = "id";
        const string FontScaleAttributeName = "font-scale";
        const string FillAttributeName = "fill";

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
        /// The name of the Bootstrap icon to use.
        /// </summary>
        [HtmlAttributeName(NameAttributeName)]
        public string Name { get; set; }

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

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "svg";
            output.TagMode = TagMode.StartTagAndEndTag;

            output.AppendCssClass("bi");

            ApplyRootAttributes(output);

            var urlHelper = ViewContext.HttpContext.RequestServices.GetService<IUrlHelper>();
            var symbol = new TagBuilder("use");
            symbol.Attributes["xlink:href"] = urlHelper.Content($"~/lib/bi/bootstrap-icons.svg#{Name}");

            output.Content.AppendHtml(symbol);
        }

        protected virtual void ApplyRootAttributes(TagHelperOutput output)
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

            if (FontScale > 0)
            {
                output.AddCssStyle("font-size", $"{FontScale.Value * 100}%");
            }
        }
    }
}
