using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Widgets;

namespace Smartstore.Core.Content.Menus
{
    public abstract class NavigationItemWithContent : NavigationItem, IHideObjectMembers
    {
        private IHtmlContent _content;
        private Widget _widget;

        public bool Ajax { get; set; }

        public AttributeDictionary ContentHtmlAttributes { get; set; } = new();

        public IHtmlContent Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    _widget = null;
                }
            }
        }

        public Widget Widget
        {
            get => _widget;
            set
            {
                if (_widget != value)
                {
                    _widget = value;
                    _content = null;
                }
            }
        }

        public bool HasContent
        {
            get => _content != null || _widget != null;
        }

        public Task<IHtmlContent> GetContentAsync(ViewContext viewContext)
        {
            Guard.NotNull(viewContext, nameof(viewContext));

            if (_content != null)
            {
                return Task.FromResult(_content);
            }

            if (_widget != null)
            {
                return _widget.InvokeAsync(viewContext);
            }

            return Task.FromResult<IHtmlContent>(null);
        }
    }
}
