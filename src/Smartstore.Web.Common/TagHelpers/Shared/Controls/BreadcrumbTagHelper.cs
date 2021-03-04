using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Widgets;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("breadcrumb", TagStructure = TagStructure.WithoutEndTag)]
    public class BreadcrumbTagHelper : SmartTagHelper 
    {
        const string TrailAttributeName = "sm-trail";

        [HtmlAttributeName(TrailAttributeName)]
        public IEnumerable<MenuItem> Trail { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();
            output.TagMode = TagMode.StartTagAndEndTag;

            var component = new ComponentWidgetInvoker("Breadcrumb", new { trail = Trail });
            var vc = await component.InvokeAsync(ViewContext);
            output.Content.SetHtmlContent(vc);
        }
    }
}
