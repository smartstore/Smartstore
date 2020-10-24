using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Scheduling;

namespace Smartstore.Core.Logging.Tasks
{
    /// <summary>
    /// A task that deletes log entries.
    /// </summary>
    public partial class DeleteLogsTask : AsyncTask
    {
        private readonly SmartDbContext _db;
        private readonly CommonSettings _commonSettings;

        public DeleteLogsTask(SmartDbContext db, CommonSettings commonSettings)
        {
            _db = db;
            _commonSettings = commonSettings;
        }

        public override Task ExecuteAsync(TaskExecutionContext ctx)
        {
            //var toUtc = DateTime.UtcNow.AddDays(-_commonSettings.MaxLogAgeInDays);

            //_logService.ClearLog(toUtc, LogLevel.Error);

            // TODO: (core) Implement DeleteLogsTask;
            return Task.CompletedTask;
        }
    }
}
