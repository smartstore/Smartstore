using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("sm-menu", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class MenuTagHelper : SmartTagHelper 
    {
        public override void Init(TagHelperContext context)
        {
            base.Init(context);

            MenuService = ViewContext.HttpContext.GetServiceScope().Resolve<IMenuService>();
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        [HtmlAttributeNotBound]
        protected IMenuService MenuService { get; set; }

        #region Properties

        [HtmlAttributeName("menu-name")]
        public string Name { get; set; }

        [HtmlAttributeName("menu-template")]
        public string Template { get; set; }

        #endregion

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCoreAsync(context, output).Await();
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            //await output.LoadAndSetChildContentAsync();

            if (!Name.HasValue() || !Template.HasValue())
            {
                output.SuppressOutput();
            }

            var menu = await MenuService.GetMenuAsync(Name);            
            var model = menu.CreateModel(Template, (ControllerContext)ActionContextAccessor.ActionContext);

            var root = model.Root;
            if (root == null)
            {
                return;
            }

            output.TagMode = TagMode.StartTagAndEndTag;
            var partial = await HtmlHelper.PartialAsync("Menus/" + Template, model);
            output.Content.AppendHtml(partial);
        }
    }
}
