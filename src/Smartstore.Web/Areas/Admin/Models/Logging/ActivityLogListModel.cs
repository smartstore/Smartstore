using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Admin.Models.Logging
{
    [LocalizedDisplay("Admin.Configuration.ActivityLog.ActivityLog.Fields.")]
    public class ActivityLogListModel : ModelBase
    {
        [LocalizedDisplay("*ActivityLogType")]
        public int ActivityLogTypeId { get; set; }
        public List<SelectListItem> ActivityLogTypes { get; set; } = new();

        [LocalizedDisplay("*CreatedOnFrom")]
        public DateTime? CreatedOnFrom { get; set; }

        [LocalizedDisplay("*CreatedOnTo")]
        public DateTime? CreatedOnTo { get; set; }

        [LocalizedDisplay("*CustomerEmail")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("*CustomerSystemAccount")]
        public bool? CustomerSystemAccount { get; set; }
    }
}
