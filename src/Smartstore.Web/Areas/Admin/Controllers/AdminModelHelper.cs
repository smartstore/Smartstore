using System;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Tasks;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Scheduling;

namespace Smartstore.Admin.Controllers
{
    /// <summary>
    /// Helper to create view models for admin area.
    /// </summary>
    public partial class AdminModelHelper
    {
        private readonly ITaskStore _taskStore;
        protected readonly IUrlHelper _urlHelper;
        protected readonly IDateTimeHelper _dateTimeHelper;

        public AdminModelHelper(
            ITaskStore taskStore, 
            IUrlHelper urlHelper, 
            IDateTimeHelper dateTimeHelper)
        {
            _taskStore = taskStore;
            _urlHelper = urlHelper;
            _dateTimeHelper = dateTimeHelper;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// Creates and prepares a task view model.
        /// </summary>
        /// <param name="taskId">Task descriptor identifier.</param>
        /// <returns>Task model.</returns>
        public async Task<TaskModel> CreateTaskModelAsync(int taskId)
        {
            if (taskId == 0)
            {
                return null;
            }

            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(taskId);
            var task = lastExecutionInfo?.Task ?? await _taskStore.GetTaskByIdAsync(taskId);

            return CreateTaskModel(task, lastExecutionInfo);
        }

        /// <summary>
        /// Creates and prepares a task view model.
        /// </summary>
        /// <param name="task">Task descriptor.</param>
        /// <param name="lastEntry">Last task execution info.</param>
        /// <returns>Task model.</returns>
        public TaskModel CreateTaskModel(TaskDescriptor task, TaskExecutionInfo lastExecutionInfo)
        {
            if (task == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var nextRunPretty = string.Empty;
            var isOverdue = false;

            TimeSpan? dueIn = task.NextRunUtc.HasValue
                ? task.NextRunUtc.Value - now
                : null;

            if (dueIn.HasValue)
            {
                if (dueIn.Value.TotalSeconds > 0)
                {
                    nextRunPretty = task.NextRunUtc.Value.Humanize(true, now);
                }
                else
                {
                    nextRunPretty = T("Common.Waiting") + "…";
                    isOverdue = true;
                }
            }

            var model = new TaskModel
            {
                Id = task.Id,
                Name = task.Name,
                CronExpression = task.CronExpression,
                CronDescription = CronExpression.GetFriendlyDescription(task.CronExpression),
                Enabled = task.Enabled,
                Priority = task.Priority,
                RunPerMachine = task.RunPerMachine,
                StopOnError = task.StopOnError,
                NextRunPretty = nextRunPretty,
                CancelUrl = _urlHelper.Action("CancelJob", "ScheduleTask", new { id = task.Id, area = "Admin" }),
                ExecuteUrl = _urlHelper.Action("RunJob", "ScheduleTask", new { id = task.Id, area = "Admin" }),
                EditUrl = _urlHelper.Action("Edit", "ScheduleTask", new { id = task.Id, area = "Admin" }),
                IsOverdue = isOverdue,
                NextRun = task.NextRunUtc.HasValue ? _dateTimeHelper.ConvertToUserTime(task.NextRunUtc.Value, DateTimeKind.Utc) : null,
                LastExecutionInfo = CreateTaskExecutionInfoModel(lastExecutionInfo)
            };

            return model;
        }

        /// <summary>
        /// Creates and prepares a task execution info model.
        /// </summary>
        /// <param name="info">Task execution info.</param>
        /// <returns>Task execution info model.</returns>
        public TaskExecutionInfoModel CreateTaskExecutionInfoModel(TaskExecutionInfo info)
        {
            if (info == null)
            {
                return new TaskExecutionInfoModel();
            }

            var startedOn = _dateTimeHelper.ConvertToUserTime(info.StartedOnUtc, DateTimeKind.Utc);

            var model = new TaskExecutionInfoModel
            {
                Id = info.Id,
                ScheduleTaskId = info.TaskDescriptorId,
                IsRunning = info.IsRunning,
                Error = info.Error.EmptyNull(),
                ProgressPercent = info.ProgressPercent,
                ProgressMessage = info.ProgressMessage,
                StartedOn = startedOn,
                StartedOnString = startedOn.ToString("g"),
                StartedOnPretty = startedOn.Humanize(false),
                MachineName = info.MachineName
            };

            if (info.FinishedOnUtc.HasValue)
            {
                model.FinishedOn = _dateTimeHelper.ConvertToUserTime(info.FinishedOnUtc.Value, DateTimeKind.Utc);
                model.FinishedOnString = model.FinishedOn.Value.ToString("g");
                model.FinishedOnPretty = model.FinishedOn.Value.Humanize(false);
            }

            if (info.SucceededOnUtc.HasValue)
            {
                model.SucceededOn = _dateTimeHelper.ConvertToUserTime(info.SucceededOnUtc.Value, DateTimeKind.Utc);
                model.SucceededOnPretty = info.SucceededOnUtc.Value.ToNativeString("G");
            }

            var durationSpan = model.IsRunning
                ? DateTime.UtcNow - info.StartedOnUtc
                : (info.FinishedOnUtc ?? info.StartedOnUtc) - info.StartedOnUtc;

            if (durationSpan > TimeSpan.Zero)
            {
                model.Duration = durationSpan.ToString("g");
            }

            return model;
        }
    }
}
