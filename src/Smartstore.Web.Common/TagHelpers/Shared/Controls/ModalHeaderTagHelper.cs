using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [OutputElementHint("div")]
    [HtmlTargetElement("modal-header", ParentTag = "modal")]
    public class ModalHeaderTagHelper : SmartTagHelper
    {
        const string TitleAttributeName = "sm-title";
        const string ShowCloseAttributeName = "sm-show-close";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);

            if (context.Items.TryGetValue(nameof(ModalTagHelper), out var obj) && obj is ModalTagHelper parent)
            {
                Parent = parent;
            }
        }

        [HtmlAttributeNotBound]
        internal ModalTagHelper Parent { get; set; }

        /// <summary>
        /// Title in modal header.
        /// </summary>
        [HtmlAttributeName(TitleAttributeName)]
        public string Title { get; set; }

        /// <summary>
        /// Whether to show close button in modal header. Default = true.
        /// </summary>
        [HtmlAttributeName(ShowCloseAttributeName)]
        public bool ShowClose { get; set; } = true;

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();

            output.TagName = "div";
            output.AppendCssClass("modal-header");

            if (!content.IsEmptyOrWhiteSpace)
            {
                output.Content.SetHtmlContent(content);
            }
            else if (Title.HasValue() || ShowClose)
            {
                if (Title.HasValue())
                {
                    output.Content.AppendHtmlLine("<h5 class='modal-title' id='{0}'>{1}</h5>".FormatCurrent(Parent.Id + "Label", Title));
                }
                if (ShowClose)
                {
                    output.Content.AppendHtmlLine("<button type='button' class='btn-close' data-dismiss='modal'><span aria-hidden='true'></span></button>");
                }
            }
            else
            {
                output.SuppressOutput();
            }
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
