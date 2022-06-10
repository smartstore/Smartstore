using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Scheduling
{
    [LocalizedDisplay("Admin.System.ScheduleTasks.")]
    public partial class TaskExecutionInfoModel : EntityModelBase
    {
        public int TaskDescriptorId { get; set; }
        public bool IsRunning { get; set; }

        [LocalizedDisplay("*LastStart")]
        [DisplayFormat(DataFormatString = "g")]
        public DateTime StartedOn { get; set; }
        public string StartedOnString { get; set; }
        public string StartedOnPretty { get; set; }

        [LocalizedDisplay("*LastEnd")]
        [DisplayFormat(DataFormatString = "g")]
        public DateTime? FinishedOn { get; set; }
        public string FinishedOnString { get; set; }
        public string FinishedOnPretty { get; set; }

        [LocalizedDisplay("*LastSuccess")]
        [DisplayFormat(DataFormatString = "g")]
        public DateTime? SucceededOn { get; set; }
        public string SucceededOnPretty { get; set; }

        [LocalizedDisplay("Common.Status")]
        public bool Succeeded => SucceededOn.HasValue && Error.IsEmpty();

        [LocalizedDisplay("Common.Error")]
        public string Error { get; set; }

        public int? ProgressPercent { get; set; }
        public string ProgressMessage { get; set; }

        [LocalizedDisplay("Common.Duration")]
        public string Duration { get; set; }

        [LocalizedDisplay("Common.MachineName")]
        public string MachineName { get; set; }
    }
}
