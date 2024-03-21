using Smartstore.ComponentModel;
using Smartstore.Core.Common.Services;
using Smartstore.Scheduling;

namespace Smartstore.Admin.Models.Scheduling
{
    internal class TaskExecutionInfoMapper : Mapper<TaskExecutionInfo, TaskExecutionInfoModel>
    {
        private readonly IDateTimeHelper _dateTimeHelper;

        public TaskExecutionInfoMapper(IDateTimeHelper dateTimeHelper)
        {
            _dateTimeHelper = dateTimeHelper;
        }

        protected override void Map(TaskExecutionInfo from, TaskExecutionInfoModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override Task MapAsync(TaskExecutionInfo from, TaskExecutionInfoModel to, dynamic parameters = null)
        {
            if (from == null || to == null)
            {
                return Task.CompletedTask;
            }

            MiniMapper.Map(from, to);

            to.Error = to.Error.EmptyNull();
            to.StartedOn = _dateTimeHelper.ConvertToUserTime(from.StartedOnUtc, DateTimeKind.Utc);
            to.StartedOnString = to.StartedOn.ToString("g");
            to.StartedOnPretty = to.StartedOn.ToHumanizedString(false);

            if (from.FinishedOnUtc.HasValue)
            {
                to.FinishedOn = _dateTimeHelper.ConvertToUserTime(from.FinishedOnUtc.Value, DateTimeKind.Utc);
                to.FinishedOnString = to.FinishedOn.Value.ToString("g");
                to.FinishedOnPretty = to.FinishedOn.Value.ToHumanizedString(false);
            }

            if (from.SucceededOnUtc.HasValue)
            {
                to.SucceededOn = _dateTimeHelper.ConvertToUserTime(from.SucceededOnUtc.Value, DateTimeKind.Utc);
                to.SucceededOnPretty = from.SucceededOnUtc.Value.ToNativeString("G");
            }

            var durationSpan = to.IsRunning
                ? DateTime.UtcNow - from.StartedOnUtc
                : (from.FinishedOnUtc ?? from.StartedOnUtc) - from.StartedOnUtc;

            if (durationSpan > TimeSpan.Zero)
            {
                to.Duration = durationSpan.ToString("g");
            }

            return Task.CompletedTask;
        }
    }
}
