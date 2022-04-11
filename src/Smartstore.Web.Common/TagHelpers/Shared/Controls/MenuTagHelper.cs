using Microsoft.AspNetCore.Http;
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

            // TODO: (mh) (core) Absolutely wrong approach! Never use session for simple modelling.
            // Create extension methods for IDisplayHelper instead (e.g. [Get|Set]xyzName)
            // and persist data in HttpContext.Items. See other IDisplayHelper extensions.
            // Refactor dependencies accordingly.
            // Besides: this feature seems useless as it is required to put the custom file into a core folder. TBD with MC.

            // Let plugin developers intercept.
            var menuComponentName = "Menu";
            var session = ViewContext.HttpContext.Session;

            if (session.ContainsKey("MainMenuCompenentName") && Name == "Main")
            {
                menuComponentName = session.GetString("MainMenuCompenentName");
            }

            var widget = new ComponentWidgetInvoker(menuComponentName, new { name = Name, template = Template });

            output.TagMode = TagMode.StartTagAndEndTag;
            //var partial = await HtmlHelper.PartialAsync("Menus/" + Template, model);
            var partial = await widget.InvokeAsync(ViewContext);
            output.Content.SetHtmlContent(partial);
        }
    }
}
