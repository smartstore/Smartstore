using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Widgets;
namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("menu", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class MenuTagHelper : SmartTagHelper 
    {
        const string NameAttributeName = "name";
        const string TemplateAttributeName = "template";

        private readonly IMenuService _menuService;

        public MenuTagHelper(IMenuService menuService)
        {
            _menuService = menuService;
        }

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

            var widget = new ComponentWidgetInvoker("Menu", new { name = Name, template = Template });

            //var menu = await _menuService.GetMenuAsync(Name);
            //if (menu == null)
            //{
            //    return;
            //}

            //var model = await menu.CreateModelAsync(Template, (ControllerContext)ActionContextAccessor.ActionContext);
            //var root = model?.Root;
            //if (root == null)
            //{
            //    return;
            //}

            output.TagMode = TagMode.StartTagAndEndTag;
            //var partial = await HtmlHelper.PartialAsync("Menus/" + Template, model);
            var partial = await widget.InvokeAsync(ViewContext);
            output.Content.SetHtmlContent(partial);
        }
    }
}
