using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Tasks;
using Smartstore.Core.Security;
using Smartstore.Scheduling;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class TaskController : AdminControllerBase
    {
        private readonly ITaskStore _taskStore;
        private readonly ITaskActivator _taskActivator;
        private readonly AdminModelHelper _adminModelHelper;

        public TaskController(
            ITaskStore taskStore,
            ITaskActivator taskActivator,
            AdminModelHelper adminModelHelper)
        {
            _taskStore = taskStore;
            _taskActivator = taskActivator;
            _adminModelHelper = adminModelHelper;
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

            var lastRun = await this.InvokeViewAsync("_LastRun", model);
            var nextRun = await this.InvokeViewAsync("_NextRun", model);

            return Json(new
            {
                lastRunHtml = lastRun,
                nextRunHtml = nextRun
            });
        }
    }
}
