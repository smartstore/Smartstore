using System.Dynamic;
using Humanizer;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Scheduling;

namespace Smartstore.Admin.Models.Scheduling
{
    internal static partial class SchedulingMappingExtensions
    {
        internal static async Task<TaskModel> MapAsync(this TaskDescriptor entity, TaskExecutionInfo lastExecutionInfo = null)
        {
            var model = new TaskModel();
            await entity.MapAsync(model, lastExecutionInfo);
            return model;
        }

        internal static async Task MapAsync(this TaskDescriptor entity, TaskModel model, TaskExecutionInfo lastExecutionInfo = null)
        {
            dynamic parameters = new ExpandoObject();
            parameters.LastExecutionInfo = lastExecutionInfo;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    internal class TaskDescriptorMapper : Mapper<TaskDescriptor, TaskModel>
    {
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IUrlHelper _urlHelper;

        public TaskDescriptorMapper(IDateTimeHelper dateTimeHelper, IUrlHelper urlHelper)
        {
            _dateTimeHelper = dateTimeHelper;
            _urlHelper = urlHelper;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(TaskDescriptor from, TaskModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(TaskDescriptor from, TaskModel to, dynamic parameters = null)
        {
            if (from == null || to == null)
            {
                return;
            }

            var lastExecutionInfo = parameters?.LastExecutionInfo as TaskExecutionInfo;
            var now = DateTime.UtcNow;
            var nextRunPretty = string.Empty;
            var isOverdue = false;

            TimeSpan? dueIn = from.NextRunUtc.HasValue
                ? from.NextRunUtc.Value - now
                : null;

            if (dueIn.HasValue)
            {
                if (dueIn.Value.TotalSeconds > 0)
                {
                    nextRunPretty = from.NextRunUtc.Value.ToHumanizedString(true, now);
                }
                else
                {
                    nextRunPretty = T("Common.Waiting") + "…";
                    isOverdue = true;
                }
            }

            MiniMapper.Map(from, to);

            to.CronDescription = CronExpression.GetFriendlyDescription(from.CronExpression);
            to.NextRunPretty = nextRunPretty;
            to.IsOverdue = isOverdue;
            to.NextRun = from.NextRunUtc.HasValue ? _dateTimeHelper.ConvertToUserTime(from.NextRunUtc.Value, DateTimeKind.Utc) : null;
            to.EditUrl = _urlHelper.Action("Edit", "Scheduling", new { id = from.Id });
            to.ExecuteUrl = _urlHelper.Action("RunJob", "Scheduling", new { id = from.Id });
            to.CancelUrl = _urlHelper.Action("CancelJob", "Scheduling", new { id = from.Id });

            if (lastExecutionInfo != null)
            {
                var executionInfoMapper = MapperFactory.GetMapper<TaskExecutionInfo, TaskExecutionInfoModel>();
                await executionInfoMapper.MapAsync(lastExecutionInfo, to.LastExecutionInfo, parameters);
            }
        }
    }
}
