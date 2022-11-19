#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Widgets
{
    public class HtmlWidget : Widget
    {
        public HtmlWidget(string html)
            : this(new HtmlString(html))
        {
        }

        public HtmlWidget(IHtmlContent content)
        {
            Content = Guard.NotNull(content, nameof(content));
        }

        public IHtmlContent Content { get; }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object? model)
            => Task.FromResult(Content);

        public override Task<IHtmlContent> Invoke2Async(WidgetContext context)
            => Task.FromResult(Content);
    }
}
