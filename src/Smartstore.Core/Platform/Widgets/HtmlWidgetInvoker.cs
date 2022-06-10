using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Widgets
{
    public class HtmlWidgetInvoker : WidgetInvoker
    {
        public HtmlWidgetInvoker(IHtmlContent content)
        {
            Content = Guard.NotNull(content, nameof(content));
        }

        public IHtmlContent Content { get; }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
            => Task.FromResult(Content);

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
            => Task.FromResult(Content);
    }
}
