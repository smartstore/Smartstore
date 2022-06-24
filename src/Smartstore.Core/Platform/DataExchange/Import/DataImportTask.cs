using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class DataImportTask : ITask
    {
        private readonly IDataImporter _importer;

        public DataImportTask(IDataImporter importer)
        {
            _importer = importer;
        }

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            var request = new DataImportRequest(ctx.ExecutionInfo.Task.Alias.ToInt())
            {
                ProgressCallback = OnProgress
            };

            // Process!
            await _importer.ImportAsync(request, cancelToken);

            Task OnProgress(int value, int max, string msg)
            {
                return ctx.SetProgressAsync(value, max, msg);
            }
        }
    }
}
