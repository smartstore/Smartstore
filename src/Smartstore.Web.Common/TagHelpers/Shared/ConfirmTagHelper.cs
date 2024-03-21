using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    public enum ConfirmActionType
    {
        Delete,
        Action
    }

    [HtmlTargetElement("confirm", Attributes = ButtonIdAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class ConfirmTagHelper : SmartTagHelper
    {
        const string ButtonIdAttributeName = "button-id";
        const string ActionAttributeName = "action";
        const string ActionUrlName = "action-url";
        const string ControllerAttributeName = "controller";
        const string ConfirmTypeAttributeName = "type";
        const string BackdropAttributeName = "backdrop";
        const string TitleAttributeName = "title";
        const string ButtonStyleAttributeName = "accept-button-color";
        const string AcceptTextAttributeName = "accept-text";
        const string CancelTextAttributeName = "cancel-text";
        const string CenterAttributeName = "center";
        const string CenterContentAttributeName = "center-content";
        const string SizeAttributeName = "size";
        const string MessageAttributeName = "message";
        const string IconClassAttributeName = "icon";
        const string IconColorAttributeName = "icon-color";

        private readonly IWidgetProvider _widgetProvider;
        private readonly IUrlHelper _urlHelper;

        public ConfirmTagHelper(IWidgetProvider widgetProvider, IUrlHelper urlHelper)
        {
            _widgetProvider = widgetProvider;
            _urlHelper = urlHelper;
        }

        /// <summary>
        /// Specifies the id of the toggle button which will be bound to the confirmation dialog.
        /// </summary>
        [HtmlAttributeName(ButtonIdAttributeName)]
        public string ButtonId { get; set; }

        /// <summary>
        /// Specifies the action to execute after accepted confirmation. Default = Delete.
        /// </summary>
        [HtmlAttributeName(ActionAttributeName)]
        public string Action { get; set; }

        /// <summary>
        /// Specifies the controller to search for the action to execute after accepted confirmation. Default = ViewContext.RouteData.Values.GetControllerName()
        /// </summary>
        [HtmlAttributeName(ControllerAttributeName)]
        public string Controller { get; set; }

        /// <summary>
        /// Specifies the URL to the target action method. 
        /// Use this property if you must transmit query string values to the action method,
        /// use <see cref="Action"/> and <see cref="Controller"/> attributes otherwise.
        /// </summary>
        [HtmlAttributeName(ActionUrlName)]
        public string ActionUrl { get; set; }

        /// <summary>
        /// Specifies the <see cref="ConfirmActionType"/>. Default = Delete.
        /// </summary>
        [HtmlAttributeName(ConfirmTypeAttributeName)]
        public ConfirmActionType ConfirmType { get; set; }

        /// <summary>
        /// Specifies whether the dialog has backdrop. Default = true.
        /// </summary>
        [HtmlAttributeName(BackdropAttributeName)]
        public bool Backdrop { get; set; } = true;

        /// <summary>
        /// Specifies the title of the dialog.
        /// </summary>
        [HtmlAttributeName(TitleAttributeName)]
        public string Title { get; set; }

        /// <summary>
        /// Specifies the custom text for the accept button.
        /// </summary>
        [HtmlAttributeName(AcceptTextAttributeName)]
        public string AcceptText { get; set; }

        /// <summary>
        /// Specifies the custom text for the cancel button.
        /// </summary>
        [HtmlAttributeName(CancelTextAttributeName)]
        public string CancelText { get; set; }

        /// <summary>
        /// Specifies the custom color for the accept button.
        /// </summary>
        [HtmlAttributeName(ButtonStyleAttributeName)]
        public ThemeColor? ButtonColor { get; set; }

        /// <summary>
        /// Specifies whether to center the dialog vertically. Default = true.
        /// </summary>
        [HtmlAttributeName(CenterAttributeName)]
        public bool Center { get; set; } = true;

        /// <summary>
        /// Specifies whether to center the dialog content. Default = false.
        /// </summary>
        [HtmlAttributeName(CenterContentAttributeName)]
        public bool CenterContent { get; set; }

        /// <summary>
        /// Specifies <see cref="ModalSize"/> of the dialog. Default = Medium.
        /// </summary>
        [HtmlAttributeName(SizeAttributeName)]
        public ModalSize Size { get; set; } = ModalSize.Medium;

        /// <summary>
        /// Specifies the custom display message.
        /// </summary>
        [HtmlAttributeName(MessageAttributeName)]
        public string Message { get; set; }

        /// <summary>
        /// Specifies the icon class.
        /// </summary>
        [HtmlAttributeName(IconClassAttributeName)]
        public string IconClass { get; set; }

        /// <summary>
        /// Specifies the custom icon color.
        /// </summary>
        [HtmlAttributeName(IconColorAttributeName)]
        public ThemeColor? IconColor { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.SuppressOutput();

            Action ??= ConfirmType == ConfirmActionType.Delete ? "Delete" : HtmlHelper.ViewContext.RouteData.Values.GetActionName();
            Controller ??= HtmlHelper.ViewContext.RouteData.Values.GetControllerName();

            var model = new ConfirmModel
            {
                ButtonId = ButtonId,
                ActionUrl = ActionUrl ?? _urlHelper.Action(Action, Controller),
                ConfirmType = ConfirmType,

                // Labels
                Title = Title,
                AcceptText = AcceptText ?? (ConfirmType == ConfirmActionType.Delete ? T("Admin.Common.Delete") : T("Common.OK")),
                CancelText = CancelText ?? (ConfirmType == ConfirmActionType.Delete ? T("Admin.Common.NoCancel") : T("Common.Cancel")),
                Message = Message ?? (ConfirmType == ConfirmActionType.Delete ? T("Admin.Common.DeleteConfirmation") : T("Admin.Common.AskToProceed")),

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

            if (ViewContext.ViewData.Model is EntityModelBase entityModel && entityModel.Id != 0 && ConfirmType == ConfirmActionType.Delete)
            {
                model.Id = entityModel.Id;
                // TODO: (mc) (core) This is really bad, but sufficient for the moment.
                model.EntityType = ButtonId.Replace("-delete", string.Empty);
            }

            // Render confirm dialog.
            var partial = await HtmlHelper.PartialAsync("Confirm", model, null);
            _widgetProvider.RegisterHtml("end", partial);
        }

        private static string GetIconClass(ConfirmActionType confirmType)
        {
            return confirmType switch
            {
                ConfirmActionType.Action => "fa fa-exclamation-circle",
                _ => "fa fa-trash-can",
            };
        }

        private static ThemeColor GetIconColor(ConfirmActionType confirmType)
        {
            return confirmType switch
            {
                ConfirmActionType.Action => ThemeColor.Warning,
                _ => ThemeColor.Danger,
            };
        }

        private static ThemeColor GetButtonStyle(ConfirmActionType confirmType)
        {
            return confirmType switch
            {
                ConfirmActionType.Action => ThemeColor.Primary,
                _ => ThemeColor.Danger,
            };
        }
    }
}