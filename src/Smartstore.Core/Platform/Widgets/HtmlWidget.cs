#nullable enable

using Microsoft.AspNetCore.Html;

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

        public override Task<IHtmlContent> InvokeAsync(WidgetContext context)
            => Task.FromResult(Content);
    }
}
