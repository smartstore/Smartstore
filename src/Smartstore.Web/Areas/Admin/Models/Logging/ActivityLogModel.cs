using Smartstore.ComponentModel;
using Smartstore.Core.Logging;

namespace Smartstore.Admin.Models.Logging
{
    [LocalizedDisplay("Admin.Configuration.ActivityLog.ActivityLog.Fields.")]
    public partial class ActivityLogModel : EntityModelBase
    {
        [LocalizedDisplay("*ActivityLogType")]
        public string ActivityLogTypeName { get; set; }

        [LocalizedDisplay("*Customer")]
        public string CustomerEmail { get; set; }
        public string CustomerEditUrl { get; set; }

        [LocalizedDisplay("*Comment")]
        public string Comment { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Admin.Customers.Customers.Fields.IsSystemAccount")]
        public bool IsSystemAccount { get; set; }
        public string SystemAccountName { get; set; }
    }

    public class ActivityLogMapper :
        IMapper<ActivityLog, ActivityLogModel>
    {
        public Task MapAsync(ActivityLog from, ActivityLogModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.ActivityLogTypeName = from.ActivityLogType?.Name;
            to.CustomerEmail = from.Customer?.Email;

            return Task.CompletedTask;
        }
    }
}