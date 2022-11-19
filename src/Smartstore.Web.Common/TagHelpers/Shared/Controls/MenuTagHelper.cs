using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("menu", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class MenuTagHelper : SmartTagHelper
    {
        const string NameAttributeName = "name";
        const string TemplateAttributeName = "template";

        [HtmlAttributeName(NameAttributeName)]
        public string Name { get; set; }

        [HtmlAttributeName(TemplateAttributeName)]
        public string Template { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            if (!Name.HasValue() || !Template.HasValue())
            {
                return;
            }

            var widget = new ComponentWidget("Menu", new { name = Name, template = Template });

            output.TagMode = TagMode.StartTagAndEndTag;
            //var partial = await HtmlHelper.PartialAsync("Menus/" + Template, model);
            var partial = await widget.InvokeAsync(ViewContext);
            output.Content.SetHtmlContent(partial);
        }
    }
}
