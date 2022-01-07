using Smartstore.Admin.Models.Scheduling;
using Smartstore.Core.Security;
using Smartstore.Scheduling;

namespace Smartstore.Admin.Components
{
    public class MinimalTaskViewComponent : SmartViewComponent
    {
        private readonly ITaskStore _taskStore;
        private readonly IPermissionService _permissionService;

        public MinimalTaskViewComponent(ITaskStore taskStore, IPermissionService permissionService)
        {
            _taskStore = taskStore;
            _permissionService = permissionService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int taskId, string returnUrl, bool cancellable = true, bool reloadPage = false)
        {
            var lastExecutionInfo = await _taskStore.GetLastExecutionInfoByTaskIdAsync(taskId);
            var task = lastExecutionInfo?.Task ?? await _taskStore.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                return Empty();
            }

            var model = await task.MapAsync(lastExecutionInfo);

            ViewBag.CanRead = await _permissionService.AuthorizeAsync(Permissions.System.ScheduleTask.Read);
            ViewBag.CanExecute = await _permissionService.AuthorizeAsync(Permissions.System.ScheduleTask.Execute);
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Cancellable = cancellable;
            ViewBag.ReloadPage = reloadPage;

            return View(model);
        }
    }
}
