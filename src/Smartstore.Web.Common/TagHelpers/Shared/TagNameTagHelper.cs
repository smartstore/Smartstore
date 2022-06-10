using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("*", Attributes = TagNameAttributeName)]
    public class TagNameTagHelper : TagHelper
    {
        const string TagNameAttributeName = "sm-tagname";

        public override int Order => int.MinValue;

        /// <summary>
        /// Changes the tag name at runtime.
        /// </summary>
        [HtmlAttributeName(TagNameAttributeName)]
        public string TagName { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (TagName.HasValue())
            {
                output.TagName = TagName;
            }
        }
    }
}
