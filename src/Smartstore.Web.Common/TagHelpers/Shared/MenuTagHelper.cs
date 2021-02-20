using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Menus;
using Smartstore.Web.Rendering.Menus;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("menu", Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class MenuTagHelper : SmartTagHelper 
    {
        const string NameAttributeName = "sm-name";
        const string TemplateAttributeName = "sm-template";

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

            var menu = await _menuService.GetMenuAsync(Name);
            if (menu == null)
            {
                return;
            }

            var model = await menu.CreateModelAsync(Template, (ControllerContext)ActionContextAccessor.ActionContext);
            var root = model?.Root;
            if (root == null)
            {
                return;
            }

            output.TagMode = TagMode.StartTagAndEndTag;
            var partial = await HtmlHelper.PartialAsync("Menus/" + Template, model);
            output.Content.SetHtmlContent(partial);
        }
    }
}
