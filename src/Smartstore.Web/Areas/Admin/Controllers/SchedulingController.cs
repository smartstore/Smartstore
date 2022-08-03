using FluentValidation.AspNetCore;
using Smartstore.Admin.Models.Scheduling;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Security;
using Smartstore.Scheduling;
using Smartstore.Threading;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class SchedulingController : AdminController
    {
        private readonly ITaskStore _taskStore;
        private readonly ITaskActivator _taskActivator;
        private readonly ITaskScheduler _taskScheduler;
        private readonly IAsyncState _asyncState;
        private readonly CommonSettings _commonSettings;

        public SchedulingController(
            ITaskStore taskStore,
            ITaskActivator taskActivator,
            ITaskScheduler taskScheduler,
            IAsyncState asyncState,
            CommonSettings commonSettings)
        {
            _taskStore = taskStore;
            _taskActivator = taskActivator;
            _taskScheduler = taskScheduler;
            _asyncState = asyncState;
            _commonSettings = commonSettings;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.ScheduleTask.Read)]
        public IActionResult List()
        {
            return View();
        }

        [Permission(Permissions.System.ScheduleTask.Read)]
        public async Task<IActionResult> TaskList()
        {
            var models = new List<TaskModel>();
            var moduleCatalog = Services.ApplicationContext.ModuleCatalog;
            var tasks = await _taskStore.GetAllTasksAsync(true, false);

            var lastExecutionInfos = (await _taskStore.GetExecutionInfoQuery(false)
                .ApplyCurrentMachineNameFilter()
                .ApplyTaskFilter(0, true)
                .ToListAsync())
                .ToDictionarySafe(x => x.TaskDescriptorId);

            foreach (var task in tasks)
            {
                var normalizedTypeName = _taskActivator.GetNormalizedTypeName(task);
                var taskType = _taskActivator.GetTaskClrType(normalizedTypeName);

                if (taskType != null)
                {
                    lastExecutionInfos.TryGetValue(task.Id, out var lastExecutionInfo);

                    var model = await task.MapAsync(lastExecutionInfo);
                    model.LastRunInfo = await InvokePartialViewAsync("_LastRun", model);
                    model.NextRunInfo = await InvokePartialViewAsync("_NextRun", model);

                    models.Add(model);
                }
            }

            var gridModel = new GridModel<TaskModel>
            {
                Rows = models,
                Total = models.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetRunningTasks()
        {
            // We better not check permission here.
            var runningExecutionInfos = await _taskStore.GetExecutionInfoQuery(false)
                .ApplyCurrentMachineNameFilter()
                .ApplyTaskFilter(0, true)
                .Where(x => x.IsRunning)
                .ToListAsync();

            if (!runningExecutionInfos.Any())
            {
                return Json(new EmptyResult());
            }

            var models = runningExecutionInfos
                .Select(x => new
                {
                    id = x.TaskDescriptorId,
                    percent = x.ProgressPercent,
                    message = x.ProgressMessage
                })
                .ToArray();

            return Json(models);
        }

        [HttpPost]
        public async Task<IActionResult> GetTaskRunInfo(int id /* taskId */)
        {
            // We better not check permission here.
            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(id);
            var task = lastExecutionInfo?.Task ?? await _taskStore.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var model = await task.MapAsync(lastExecutionInfo);
            var lastRunHtml = await InvokePartialViewAsync("_LastRun", model);
            var nextRunHtml = await InvokePartialViewAsync("_NextRun", model);

            return Json(new { lastRunHtml, nextRunHtml });
        }

        [Permission(Permissions.System.ScheduleTask.Execute)]
        public async Task<IActionResult> RunJob(int id, string returnUrl)
        {
            var taskParams = new Dictionary<string, string>
            {
                { TaskExecutor.CurrentCustomerIdParamName, Services.WorkContext.CurrentCustomer.Id.ToString() },
                { TaskExecutor.CurrentStoreIdParamName, Services.StoreContext.CurrentStore.Id.ToString() }
            };

            _ = _taskScheduler.RunSingleTaskAsync(id, taskParams);

            // The most tasks are completed rather quickly. Wait a while...
            await Task.Delay(200);

            // ...check and return suitable notifications.
            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(id);
            if (lastExecutionInfo != null)
            {
                if (lastExecutionInfo.IsRunning)
                {
                    NotifyInfo(await GetTaskMessage(lastExecutionInfo.Task, "Admin.System.ScheduleTasks.RunNow.Progress"));
                }
                else if (lastExecutionInfo.Error.HasValue())
                {
                    NotifyError(lastExecutionInfo.Error);
                }
                else
                {
                    NotifySuccess(await GetTaskMessage(lastExecutionInfo.Task, "Admin.System.ScheduleTasks.RunNow.Success"));
                }
            }

            return RedirectToReferrer(returnUrl, () => RedirectToAction(nameof(List)));
        }

        [Permission(Permissions.System.ScheduleTask.Execute)]
        public IActionResult CancelJob(int id /* taskId */, string returnUrl)
        {
            if (_asyncState.Cancel<TaskDescriptor>(id.ToString()))
            {
                NotifyWarning(T("Admin.System.ScheduleTasks.CancellationRequested"));
            }

            return RedirectToReferrer(returnUrl);
        }

        [Permission(Permissions.System.ScheduleTask.Read)]
        public async Task<IActionResult> Edit(int id /* taskId */, string returnUrl = "")
        {
            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(id);
            var task = lastExecutionInfo?.Task ?? await _taskStore.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var model = await task.MapAsync(lastExecutionInfo);

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.HistoryCleanupNote = T("Admin.System.ScheduleTasks.HistoryCleanupNote").Value.FormatInvariant(
                _commonSettings.MaxNumberOfScheduleHistoryEntries,
                _commonSettings.MaxScheduleHistoryAgeInDays);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.System.ScheduleTask.Update)]
        public async Task<IActionResult> Edit([CustomizeValidator(RuleSet = "TaskEditing")] TaskModel model, bool continueEditing, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var task = await _taskStore.GetTaskByIdAsync(model.Id);
            if (task == null)
            {
                return NotFound();
            }

            task.Name = model.Name;
            task.Enabled = model.Enabled;
            task.StopOnError = model.StopOnError;
            task.CronExpression = model.CronExpression;
            task.Priority = model.Priority;
            task.NextRunUtc = model.Enabled
                ? _taskStore.GetNextSchedule(task)
                : null;

            await _taskStore.UpdateTaskAsync(task);

            NotifySuccess(T("Admin.System.ScheduleTasks.UpdateSuccess"));

            if (continueEditing)
            {
                return RedirectToAction(nameof(Edit), new { id = model.Id, returnUrl });
            }
            else if (returnUrl.HasValue())
            {
                return RedirectToReferrer(returnUrl, () => RedirectToAction(nameof(List)));
            }

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.System.ScheduleTask.Read)]
        public IActionResult FutureSchedules(string expression)
        {
            try
            {
                var now = DateTime.Now;
                var model = CronExpression.GetFutureSchedules(expression, now, now.AddYears(1), 20);
                ViewBag.Description = CronExpression.GetFriendlyDescription(expression);
                return PartialView(model);
            }
            catch (Exception ex)
            {
                ViewBag.CronScheduleParseError = ex.Message;
                return PartialView(Enumerable.Empty<DateTime>());
            }
        }

        [Permission(Permissions.System.ScheduleTask.Read)]
        public async Task<IActionResult> TaskExecutionInfoList(GridCommand command, int id /* taskId */)
        {
            var query = _taskStore.GetExecutionInfoQuery(false)
                .ApplyTaskFilter(id)
                .ApplyGridCommand(command);

            var infos = await query.ToPagedList(command).LoadAsync();
            var infoMapper = MapperFactory.GetMapper<TaskExecutionInfo, TaskExecutionInfoModel>();

            var rows = await infos.SelectAwait(async x =>
            {
                var infoModel = new TaskExecutionInfoModel();
                await infoMapper.MapAsync(x, infoModel);
                return infoModel;
            })
            .AsyncToList();

            return Json(new GridModel<TaskExecutionInfoModel>
            {
                Rows = rows,
                Total = infos.TotalCount,
            });
        }

        [HttpPost]
        [Permission(Permissions.System.ScheduleTask.Delete)]
        public async Task<IActionResult> TaskExecutionInfoDelete(GridSelection selection)
        {
            var numDeleted = await _taskStore.DeleteExecutionInfosByIdsAsync(selection.GetEntityIds());

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        public async Task<IActionResult> MinimalTask(int taskId, string returnUrl /* mandatory on purpose */)
        {
            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(taskId);
            var task = lastExecutionInfo?.Task ?? await _taskStore.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                return new EmptyResult();
            }

            var model = await task.MapAsync(lastExecutionInfo);

            ViewBag.CanRead = await Services.Permissions.AuthorizeAsync(Permissions.System.ScheduleTask.Read);
            ViewBag.CanExecute = await Services.Permissions.AuthorizeAsync(Permissions.System.ScheduleTask.Execute);
            ViewBag.ReturnUrl = returnUrl;

            return PartialView(model);
        }

        private async Task<string> GetTaskMessage(TaskDescriptor task, string resourceKey)
        {
            var normalizedTypeName = _taskActivator.GetNormalizedTypeName(task);

            string message = normalizedTypeName.HasValue()
                ? await Services.Localization.GetResourceAsync(resourceKey + "." + normalizedTypeName, logIfNotFound: false, returnEmptyIfNotFound: true)
                : null;

            return message.IsEmpty() ? T(resourceKey) : message;
        }
    }
}
