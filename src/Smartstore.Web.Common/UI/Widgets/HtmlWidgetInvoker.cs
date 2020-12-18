using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Web.UI
{
    public class HtmlWidgetInvoker : WidgetInvoker
    {
        private readonly IHtmlContent _html;

        public HtmlWidgetInvoker(IHtmlContent html)
        {
            Guard.NotNull(html, nameof(html));
            _html = html;
        }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
        {
            return Task.FromResult(_html);
        }
    }
}
