using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement("input", Attributes = "asp-for, sm-is-search-expression", TagStructure = TagStructure.WithoutEndTag)]
    public class IsSearchExpressionTagHelper : BaseFormTagHelper
    {
        public override int Order => 200;

        [HtmlAttributeName("sm-is-search-expression")]
        public bool IsSearchExpression { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (!IsSearchExpression)
            {
                return;
            }

            if (!output.Attributes.TryGetAttribute("type", out var typeAttr) || typeAttr.ValueAsString() != "text")
            {
                return;
            }

            var url = UrlHelper.Action("SearchFilter", "Help", new { area = "Admin" });
            output.PostElement.AppendHtml(@$"
<span class='input-group-icon'>
    <a href='{url}' class='popup-toggle search-expression-toggle'>
        <i class='fa fa-circle-question'></i>
    </a>
</span>");

            output.PreElement.AppendHtml("<div class='has-icon has-icon-right'>");
            output.PostElement.AppendHtml("</div>");
        }
    }
}
