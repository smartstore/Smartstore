using System;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Localization;
using Smartstore.Scheduling;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Tasks
{
    [LocalizedDisplay("Admin.System.ScheduleTasks.")]
    public partial class TaskModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*CronExpression")]
        public string CronExpression { get; set; }
        public string CronDescription { get; set; }

        [LocalizedDisplay("*Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("*Priority")]
        public TaskPriority Priority { get; set; }

        [LocalizedDisplay("*RunPerMachine")]
        public bool RunPerMachine { get; set; }

        [LocalizedDisplay("*StopOnError")]
        public bool StopOnError { get; set; }

        [LocalizedDisplay("*NextRun")]
        [DisplayFormat(DataFormatString = "g")]
        public DateTime? NextRun { get; set; }
        public string NextRunPretty { get; set; }

        public bool IsOverdue { get; set; }

        public string CancelUrl { get; set; }
        public string EditUrl { get; set; }
        public string ExecuteUrl { get; set; }

        public TaskExecutionInfoModel LastExecutionInfo { get; set; }
    }

    public partial class TaskValidator : AbstractValidator<TaskModel>
    {
        public TaskValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.CronExpression).Must(x => CronExpression.IsValid(x)).WithMessage(T("Admin.System.ScheduleTasks.InvalidCronExpression"));
        }
    }
}
