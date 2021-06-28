using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Tasks;
using Smartstore.Core.Security;
using Smartstore.Scheduling;
using Smartstore.Threading;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class TaskController : AdminControllerBase
    {
        private readonly ITaskStore _taskStore;
        private readonly ITaskActivator _taskActivator;
        private readonly ITaskScheduler _taskScheduler;
        private readonly AdminModelHelper _adminModelHelper;
        private readonly IAsyncState _asyncState;

        public TaskController(
            ITaskStore taskStore,
            ITaskActivator taskActivator,
            ITaskScheduler taskScheduler,
            AdminModelHelper adminModelHelper,
            IAsyncState asyncState)
        {
            _taskStore = taskStore;
            _taskActivator = taskActivator;
            _taskScheduler = taskScheduler;
            _adminModelHelper = adminModelHelper;
            _asyncState = asyncState;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.System.ScheduleTask.Read)]
        public async Task<IActionResult> List()
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

                if (moduleCatalog.IsActiveModuleAssembly(taskType?.Assembly))
                {
                    lastExecutionInfos.TryGetValue(task.Id, out var lastExecutionInfo);

                    var model = _adminModelHelper.CreateTaskModel(task, lastExecutionInfo);
                    if (model != null)
                    {
                        models.Add(model);
                    }
                }
            }

            return View(models);
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
            var model = await _adminModelHelper.CreateTaskModelAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            var lastRunHtml = await this.InvokeViewAsync("_LastRun", model);
            var nextRunHtml = await this.InvokeViewAsync("_NextRun", model);

            return Json(new { lastRunHtml, nextRunHtml });
        }

        [Permission(Permissions.System.ScheduleTask.Execute)]
        public async Task<IActionResult> RunJob(int id, string returnUrl = "")
        {
            var taskParams = new Dictionary<string, string>
            {
                { TaskExecutor.CurrentCustomerIdParamName, Services.WorkContext.CurrentCustomer.Id.ToString() },
                { TaskExecutor.CurrentStoreIdParamName, Services.StoreContext.CurrentStore.Id.ToString() }
            };

            await _taskScheduler.RunSingleTaskAsync(id, taskParams);

            // The most tasks are completed rather quickly. Wait a while...
            Thread.Sleep(200);

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

            return RedirectToReferrer(returnUrl, () => RedirectToAction("List"));
        }

        [Permission(Permissions.System.ScheduleTask.Execute)]
        public IActionResult CancelJob(int id /* taskId */, string returnUrl = "")
        {
            if (_asyncState.Cancel<TaskDescriptor>(id.ToString()))
            {
                NotifyWarning(T("Admin.System.ScheduleTasks.CancellationRequested"));
            }

            return RedirectToReferrer(returnUrl);
        }

        // TODO: (mg) (core) POST TaskController.Edit\Create requires validation rule set.
        // [CustomizeValidator(RuleSet = "TaskEditing")]

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
