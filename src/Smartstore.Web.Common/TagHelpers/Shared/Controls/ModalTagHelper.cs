using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

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

    public enum ModalBackdrop
    {
        /// <summary>
        /// Enables default backdrop.
        /// </summary>
        Show,

        /// <summary>
        /// No backdrop.
        /// </summary>
        Hide,

        /// <summary>
        /// Enables default backdrop, but does not close on backdrop click.
        /// </summary>
        Static,

        /// <summary>
        /// Enables inverted (white) backdrop.
        /// </summary>
        Inverse,

        /// <summary>
        /// Enables backdrop, but makes it invisible.
        /// </summary>
        Invisible
    }

    [HtmlTargetElement("modal", Attributes = "id")]
    [OutputElementHint("div")]
    [RestrictChildren("modal-header", "modal-body", "modal-footer")]
    public class ModalTagHelper : SmartTagHelper
    {
        const string SizeAttributeName = "sm-size";
        const string FadeAttributeName = "sm-fade";
        const string FocusAttributeName = "sm-focus";
        const string BackdropAttributeName = "sm-backdrop";
        const string ShowAttributeName = "sm-show";
        const string CloseOnEscapePressAttributeName = "sm-close-on-escape-press";
        const string CenterVerticallyAttributeName = "sm-center-vertically";
        const string CenterContentAttributeName = "sm-center-content";
        const string RenderAtPageEndAttributeName = "sm-render-at-page-end";

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
        [HtmlAttributeName(SizeAttributeName)]
        public ModalSize Size { get; set; } = ModalSize.Medium;

        /// <summary>
        /// Whether to activate fade animations. Default = true.
        /// </summary>
        [HtmlAttributeName(FadeAttributeName)]
        public bool Fade { get; set; } = true;

        /// <summary>
        /// Whether to focus modal. Default = true.
        /// </summary>
        [HtmlAttributeName(FocusAttributeName)]
        public bool Focus { get; set; } = true;

        /// <summary>
        /// Specifies backdrop kind. Default = <see cref="ModalBackdrop.Show"/>.
        /// </summary>
        [HtmlAttributeName(BackdropAttributeName)]
        public ModalBackdrop Backdrop { get; set; } = ModalBackdrop.Show;

        /// <summary>
        /// Whether to initially show modal. Default = true.
        /// </summary>
        [HtmlAttributeName(ShowAttributeName)]
        public bool Show { get; set; } = true;

        /// <summary>
        /// Whether to close modal on ESC press. Default = true.
        /// </summary>
        [HtmlAttributeName(CloseOnEscapePressAttributeName)]
        public bool CloseOnEscapePress { get; set; } = true;

        /// <summary>
        /// Whether to center modal vertically. Default = false.
        /// </summary>
        [HtmlAttributeName(CenterVerticallyAttributeName)]
        public bool CenterVertically { get; set; }

        /// <summary>
        /// Whether to center dialog content. Default = false.
        /// </summary>
        [HtmlAttributeName(CenterContentAttributeName)]
        public bool CenterContent { get; set; } = false;

        /// <summary>
        /// Whether to render modal at page end (right before closing body tag). Default = true.
        /// </summary>
        [HtmlAttributeName(RenderAtPageEndAttributeName)]
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

            if (CenterContent)
            {
                output.AppendCssClass("modal-box");
            }

            var backdrop = "true";
            if (Backdrop == ModalBackdrop.Hide)
            {
                backdrop = "false";
            }
            else if (Backdrop == ModalBackdrop.Static)
            {
                backdrop = "static";
            }
            else if (Backdrop == ModalBackdrop.Inverse)
            {
                backdrop = "invert";
            }
            else if (Backdrop == ModalBackdrop.Invisible)
            {
                backdrop = "invisible";
            }

            output.MergeAttribute("role", "dialog");
            output.MergeAttribute("tabindex", -1);
            output.MergeAttribute("aria-hidden", "true");
            output.MergeAttribute("aria-labelledby", Id + "Label");
            output.MergeAttribute("data-keyboard", CloseOnEscapePress.ToString().ToLower());
            output.MergeAttribute("data-show", Show.ToString().ToLower());
            output.MergeAttribute("data-focus", Focus.ToString().ToLower());
            output.MergeAttribute("data-backdrop", backdrop);

            // .modal-dialog
            BuildDialog(output);

            // .modal-content
            BuildContent(output);

            if (RenderAtPageEnd && !ViewContext.HttpContext.Request.IsAjax())
            {
                // Move output Html to new builder
                var builder = new SmartHtmlContentBuilder();
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

            if (CenterContent)
            {
                className += " modal-box-center";
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