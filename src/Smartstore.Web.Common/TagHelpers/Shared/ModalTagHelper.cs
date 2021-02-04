using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Widgets;

namespace Smartstore.Web.TagHelpers.Shared
{
    public enum ModalSize
    {
        Small,
        Medium,
        Large,
        Flex,
        FlexSmall
    }

    [OutputElementHint("div")]
    [RestrictChildren("modal-header", "modal-body", "modal-footer")]
    [HtmlTargetElement("modal", Attributes = "id")]
    public class ModalTagHelper : SmartTagHelper
    {
        private readonly IWidgetProvider _widgetProvider;

        public ModalTagHelper(IWidgetProvider widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
            context.Items[nameof(ModalTagHelper)] = this;
        }

        /// <summary>
        /// Size of modal. Default = Medium.
        /// </summary>
        public ModalSize Size { get; set; } = ModalSize.Medium;

        /// <summary>
        /// Whether to activate fade animations. Default = true.
        /// </summary>
        public bool Fade { get; set; } = true;

        /// <summary>
        /// Whether to focus modal. Default = true.
        /// </summary>
        public bool Focus { get; set; } = true;

        /// <summary>
        /// Whether to render modal backdrop. Default = true.
        /// </summary>
        public bool Backdrop { get; set; } = true;

        /// <summary>
        /// Whether to initially show modal. Default = true.
        /// </summary>
        public bool Show { get; set; } = true;

        /// <summary>
        /// Whether to close modal on ESC press. Default = true.
        /// </summary>
        public bool CloseOnEscapePress { get; set; } = true;

        /// <summary>
        /// Whether to close modal on backdrop click. Default = true.
        /// </summary>
        public bool CloseOnBackdropClick { get; set; } = true;

        /// <summary>
        /// Whether to center modal vertically. Default = false.
        /// </summary>
        public bool CenterVertically { get; set; }

        /// <summary>
        /// Whether to render modal at page end (right before closing body tag). Default = true.
        /// </summary>
        public bool RenderAtPageEnd { get; set; } = true;

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await output.LoadAndSetChildContentAsync();

            output.TagName = "div";
            output.AppendCssClass("modal");
            if (Fade)
            {
                output.AppendCssClass("fade");
            }

            output.MergeAttribute("role", "dialog");
            output.MergeAttribute("tabindex", -1);
            output.MergeAttribute("aria-hidden", "true");
            output.MergeAttribute("aria-labelledby", Id + "Label");
            output.MergeAttribute("data-keyboard", CloseOnEscapePress.ToString().ToLower());
            output.MergeAttribute("data-show", Show.ToString().ToLower());
            output.MergeAttribute("data-focus", Focus.ToString().ToLower());
            output.MergeAttribute("data-backdrop", Backdrop ? (CloseOnBackdropClick ? "true" : "static") : "false");

            // .modal-dialog
            BuildDialog(output);

            // .modal-content
            BuildContent(output);

            if (RenderAtPageEnd)
            {
                // Move output Html to new builder
                var builder = new HtmlContentBuilder();
                ((IHtmlContentContainer)output).MoveTo(builder);
                
                _widgetProvider.RegisterHtml("end", builder);
                output.SuppressOutput();
            }
        }

        private TagBuilder BuildDialog(TagHelperOutput output)
        {
            TagBuilder div = new("div");

            var className = "modal-dialog";
            switch (Size)
            {
                case ModalSize.Small:
                    className += " modal-sm";
                    break;
                case ModalSize.Large:
                    className += " modal-lg";
                    break;
                case ModalSize.Flex:
                    className += " modal-flex";
                    break;
                case ModalSize.FlexSmall:
                    className += " modal-flex modal-flex-sm";
                    break;
            }

            if (CenterVertically)
            {
                className += " modal-dialog-centered";
            }

            div.Attributes["class"] = className;
            div.Attributes["role"] = "document";

            output.PreContent.AppendHtml(div.RenderStartTag());
            output.PostContent.AppendHtml(div.RenderEndTag());

            return div;
        }

        private TagBuilder BuildContent(TagHelperOutput output)
        {
            TagBuilder div = new("div");

            div.Attributes["class"] = "modal-content";

            output.PreContent.AppendHtml(div.RenderStartTag());
            output.PostContent.AppendHtml(div.RenderEndTag());

            return div;
        }
    }
}