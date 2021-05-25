using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class DataImportTask : ITask
    {
        private readonly SmartDbContext _db;
        private readonly IDataImporter _importer;

        public DataImportTask(SmartDbContext db, IDataImporter importer)
        {
            _db = db;
            _importer = importer;
        }

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            var profileId = ctx.ExecutionInfo.Task.Alias.ToInt();
            var profile = await _db.ImportProfiles.FindByIdAsync(profileId, true, cancelToken);

            var request = new DataImportRequest(profile)
            {
                ProgressCallback = OnProgress
            };

            // Process!
            await _importer.ImportAsync(request, cancelToken);

            Task OnProgress(int value, int max, string msg)
            {
                return ctx.SetProgressAsync(value, max, msg, true);
            }
        }
    }
}
