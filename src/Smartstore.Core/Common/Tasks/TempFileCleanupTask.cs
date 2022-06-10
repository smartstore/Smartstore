using Smartstore.Scheduling;

namespace Smartstore.Core.Common.Tasks
{
    /// <summary>
    /// Task to cleanup temporary files.
    /// </summary>
    public partial class TempFileCleanupTask : ITask
    {
        private readonly IApplicationContext _appContext;

        public TempFileCleanupTask(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            var olderThan = TimeSpan.FromHours(5);

            var tempDir = _appContext.GetTempDirectory();
            if (tempDir.Exists)
            {
                tempDir.FileSystem.ClearDirectory(tempDir, false, olderThan);
            }

            var tenantTempDir = _appContext.GetTenantTempDirectory();
            if (tenantTempDir.Exists)
            {
                tenantTempDir.FileSystem.ClearDirectory(tenantTempDir, false, olderThan);
            }

            return Task.CompletedTask;
        }
    }
}
