using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Widgets
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
            => Task.FromResult(_html);

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
            => Task.FromResult(_html);
    }
}
