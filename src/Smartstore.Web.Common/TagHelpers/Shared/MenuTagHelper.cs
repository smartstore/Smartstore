using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Web.Rendering;
using Smartstore.Web.TagHelpers;

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

        [HtmlAttributeName("Route")]
        public RouteInfo Route { get; set; }
        
        #endregion

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCoreAsync(context, output).Await();
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.LoadAndSetChildContentAsync();

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

            #region OBSOLETE Test

            //var isFirst = true;
            //var hasIcons = root.Children.Any(x => x.Value.Icon.HasValue());
            //var hasImages = root.Children.Any(x => x.Value.ImageUrl.HasValue());
            //// TODO: (mh) (core) Respect rtl...
            ////var rtl = WorkContext.WorkingLanguage.Rtl;

            //// INFO: This is for testing only. Not the final impplementation!!! Please do not review yet.
            //// TODO: (mh) (core) Finish the job...
            //// TODO: (mh) (core) Implement seperate Taghelpers for each MenuType. Maybe according to MediaTagHelper...
            //if (Template == "LinkList")
            //{
            //    output.AppendCssClass("cms-menu cms-menu-linklist");
            //    output.Attributes.Add("data-menu-name", model.Name?.ToLower());

            //    var list = new TagBuilder("ul");
            //    list.AppendCssClass("list-unstyled");

            //    foreach (var node in root.Children)
            //    {
            //        var item = node.Value;
            //        if (!item.Visible)
            //        {
            //            continue;
            //        }

            //        var itemText = node.GetItemText(T);
            //        var itemUrl = item.GenerateUrl(ViewContext);

            //        var li = new TagBuilder("li");

            //        if (item.IsGroupHeader)
            //        {
            //            if (!isFirst)
            //            {
            //                var hr = new TagBuilder("hr");
            //                hr.AppendCssClass("my-2");
            //                li.InnerHtml.AppendHtml(hr);
            //            }
            //            if (itemText.HasValue() && item.Text != "[SKIP]")
            //            {
            //                li.AppendCssClass("menu-header h5 mt-2");
            //                li.InnerHtml.Append(itemText);
            //            }
            //            isFirst = false;
            //            list.InnerHtml.AppendHtml(li);
            //            continue;
            //        }

            //        var attrs = item.GetCombinedAttributes().PrependCssClass("menu-link");

            //        var a = new TagBuilder("a");
            //        attrs.Add("href", itemUrl);
            //        a.Attributes.AddRange(attrs);

            //        // TODO: (mh) (core) Icons

            //        var span = new TagBuilder("span");
            //        span.InnerHtml.Append(itemText);

            //        a.InnerHtml.AppendHtml(span);
            //        li.InnerHtml.AppendHtml(a);
            //        list.InnerHtml.AppendHtml(li);
            //    }

            //    output.Content.AppendHtml(list);
            //}

            #endregion

            // INFO: This code can render views. Maybe this will come in handy for MegaMenu.
            var sw = new StringWriter();
            ViewDataDictionary viewData = new(ViewContext.ViewData);
            viewData.Model = model;
            var viewContext = new ViewContext(ViewContext, ViewContext.View, viewData, ViewContext.TempData, sw, new HtmlHelperOptions ());
            output.Content.AppendHtml(await RenderPartialView(viewContext, Template));
        }

        public async static Task<string> RenderPartialView(ViewContext context, string viewName, ICompositeViewEngine viewEngine = null, ViewEngineResult viewResult = null)
        {
            viewEngine ??= context.HttpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
            viewResult ??= viewEngine.FindView(context, viewName, false);
            await viewResult.View.RenderAsync(context);

            //var viewEngine2 = context.HttpContext.RequestServices.GetRequiredService<IRazorViewEngine>();
            //var result2 = viewEngine2.FindView(context, viewName, false);
            //await result2.View.RenderAsync(context);

            return context.Writer.ToString();
        }
    }
}
