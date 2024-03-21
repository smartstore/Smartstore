using Smartstore.Web.Modelling;
using Smartstore.Web.TagHelpers.Shared;

namespace Smartstore.Web.Rendering
{
    public partial class ConfirmModel : ModelBase
    {
        public string ButtonId { get; set; }
        public string ActionUrl { get; set; }
        public ConfirmActionType ConfirmType { get; set; }
        public int Id { get; set; }
        public string EntityType { get; set; }
        public bool Backdrop { get; set; }
        public string Title { get; set; }
        public ThemeColor ButtonStyle { get; set; }
        public string AcceptText { get; set; }
        public string CancelText { get; set; }
        public bool Center { get; set; }
        public bool CenterContent { get; set; }
        public ModalSize Size { get; set; } = ModalSize.Medium;
        public string Message { get; set; }
        public string IconClass { get; set; }
        public ThemeColor IconColor { get; set; }
    }
}
