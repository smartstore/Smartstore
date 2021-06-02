using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Widgets;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    public enum ConfirmType
    {
        Delete,
        Action
    }

    [OutputElementHint("button")]
    [HtmlTargetElement("confirm", Attributes = ButtonIdAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class ConfirmTagHelper : SmartTagHelper
    {
        const string ButtonIdAttributeName = "button-id";
        const string ActionAttributeName = "action";
        const string ControllerAttributeName = "controller";
        const string ConfirmTypeAttributeName = "confirm-type";
        const string BackdropAttributeName = "backdrop";
        const string TitleAttributeName = "title";
        const string ButtonStyleAttributeName = "accept-button-color";
        const string AcceptTextAttributeName = "accept-text";
        const string CancelTextAttributeName = "cancel-text";
        const string CenterAttributeName = "center";
        const string CenterContentAttributeName = "center-content";
        const string SizeAttributeName = "size";
        const string MessageAttributeName = "message";
        const string IconClassAttributeName = "icon-class";
        const string IconColorAttributeName = "icon-color";

        private readonly IWidgetProvider _widgetProvider;
        private readonly IUrlHelper _urlHelper;
        
        public ConfirmTagHelper(IWidgetProvider widgetProvider, IUrlHelper urlHelper)
        {
            _widgetProvider = widgetProvider;
            _urlHelper = urlHelper;
        }

        /// <summary>
        /// Specifies the id of the button which will be bound to the confirmation dialog.
        /// </summary>
        [HtmlAttributeName(ButtonIdAttributeName)]
        public string ButtonId { get; set; }

        /// <summary>
        /// Specifies the action to execute after confirmation.
        /// </summary>
        [HtmlAttributeName(ActionAttributeName)]
        public string Action { get; set; }

        /// <summary>
        /// Specifies the controller to search for the action to execute after confirmation.
        /// </summary>
        [HtmlAttributeName(ControllerAttributeName)]
        public string Controller { get; set; }

        /// <summary>
        /// Specifies the <see cref="ConfirmType"/>. Delete || Action
        /// </summary>
        [HtmlAttributeName(ConfirmTypeAttributeName)]
        public ConfirmType ConfirmType { get; set; }

        /// <summary>
        /// Specifies whether the dialog has backdrop.
        /// </summary>
        [HtmlAttributeName(BackdropAttributeName)]
        public bool Backdrop { get; set; } = true;

        /// <summary>
        /// Specifies the title of the dialog.
        /// </summary>
        [HtmlAttributeName(TitleAttributeName)]
        public string Title { get; set; }

        /// <summary>
        /// Specifies the text for the accept button.
        /// </summary>
        [HtmlAttributeName(AcceptTextAttributeName)]
        public string AcceptText { get; set; }

        /// <summary>
        /// Specifies the text for the cancel button.
        /// </summary>
        [HtmlAttributeName(CancelTextAttributeName)]
        public string CancelText { get; set; }

        /// <summary>
        /// Specifies the color for the accept button.
        /// </summary>
        [HtmlAttributeName(ButtonStyleAttributeName)]
        public ButtonStyle? ButtonColor { get; set; }

        /// <summary>
        /// Specifies whether to center the dialog vertically.
        /// </summary>
        [HtmlAttributeName(CenterAttributeName)]
        public bool Center { get; set; }

        /// <summary>
        /// Specifies whether to center the dialog content.
        /// </summary>
        [HtmlAttributeName(CenterContentAttributeName)]
        public bool CenterContent { get; set; }

        /// <summary>
        /// Specifies <see cref="ModalSize"/> of the dialog.
        /// </summary>
        [HtmlAttributeName(SizeAttributeName)]
        public ModalSize Size { get; set; } = ModalSize.Medium;

        /// <summary>
        /// Specifies the display message.
        /// </summary>
        [HtmlAttributeName(MessageAttributeName)]
        public string Message { get; set; }

        /// <summary>
        /// Specifies the icon class.
        /// </summary>
        [HtmlAttributeName(IconClassAttributeName)]
        public string IconClass { get; set; }

        /// <summary>
        /// Specifies the icon color.
        /// </summary>
        [HtmlAttributeName(IconColorAttributeName)]
        public BadgeStyle? IconColor { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            Action ??= ConfirmType == ConfirmType.Delete ? "Delete" : HtmlHelper.ViewContext.RouteData.Values.GetActionName();
            Controller ??= HtmlHelper.ViewContext.RouteData.Values.GetControllerName();

            var model = new ConfirmModel
            {
                ButtonId = ButtonId,
                FormPostUrl = _urlHelper.Action(Action, Controller),
                ConfirmType = ConfirmType,

                // Labels
                Title = Title,
                AcceptText = AcceptText ?? (ConfirmType == ConfirmType.Delete ? T("Admin.Common.Delete") : T("Common.OK")),
                CancelText = CancelText ?? (ConfirmType == ConfirmType.Delete ? T("Admin.Common.NoCancel") : T("Common.Cancel")),
                Message = Message ?? (ConfirmType == ConfirmType.Delete ? T("Admin.Common.DeleteConfirmation") : T("Admin.Common.AskToProceed")),

                // Modal setings
                Backdrop = Backdrop,
                Center = Center,
                CenterContent = CenterContent,
                Size = Size,

                // Icon class & color
                IconClass = IconClass ?? GetIconClass(ConfirmType),
                IconColor = IconColor ?? GetIconColor(ConfirmType),
                ButtonStyle = ButtonColor ?? GetButtonStyle(ConfirmType)
            };

            if (ViewContext.ViewData.Model is EntityModelBase entityModel && entityModel.Id != 0 && ConfirmType == ConfirmType.Delete)
            {
                model.Id = entityModel.Id;
                // TODO: (MC) This is really bad, but sufficient for the moment.
                model.EntityType = ButtonId.Replace("-delete", string.Empty);
            }

            // Render confirm dialog.
            var partial = await HtmlHelper.PartialAsync("Confirm", model, null);
            var widget = new HtmlWidgetInvoker(partial);
            _widgetProvider.RegisterWidget("end", widget);
        }
    
        private static string GetIconClass(ConfirmType confirmType)
        {
            return confirmType switch
            {
                ConfirmType.Action => "fa fa-exclamation-circle",
                _ => "fa fa-trash-alt",
            };
        }

        private static BadgeStyle GetIconColor(ConfirmType confirmType)
        {
            return confirmType switch
            {
                ConfirmType.Action => BadgeStyle.Warning,
                _ => BadgeStyle.Danger,
            };
        }

        private static ButtonStyle GetButtonStyle(ConfirmType confirmType)
        {
            return confirmType switch
            {
                ConfirmType.Action => ButtonStyle.Primary,
                _ => ButtonStyle.Danger,
            };
        }
    }
}