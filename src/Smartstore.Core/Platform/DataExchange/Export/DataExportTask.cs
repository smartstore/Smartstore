using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExportTask : ITask
    {
        private readonly SmartDbContext _db;
        private readonly IDataExporter _dataExporter;
        private readonly IProviderManager _providerManager;

        public DataExportTask(SmartDbContext db, IDataExporter dataExporter, IProviderManager providerManager)
        {
            _db = db;
            _dataExporter = dataExporter;
            _providerManager = providerManager;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default)
        {
            var profileId = ctx.ExecutionInfo.Task.Alias.ToInt();
            var profile = await _db.ExportProfiles
                .Include(x => x.Deployments)
                .FindByIdAsync(profileId, true, cancelToken);

            if (profile == null)
            {
                return;
            }

            var provider = _providerManager.GetProvider<IExportProvider>(profile.ProviderSystemName);
            if (provider == null)
            {
                throw new InvalidOperationException(T("Admin.Common.ProviderNotLoaded", profile.ProviderSystemName.NaIfEmpty()));
            }

            // Create export request.
            var request = new DataExportRequest(profile, provider)
            {
                ProgressCallback = OnProgress
            };

            if (ctx.Parameters.ContainsKey("SelectedIds"))
            {
                request.EntitiesToExport = ctx.Parameters["SelectedIds"]
                    .SplitSafe(',')
                    .Select(x => x.ToInt())
                    .ToList();
            }

            if (ctx.Parameters.ContainsKey("ActionOrigin"))
            {
                request.ActionOrigin = ctx.Parameters["ActionOrigin"];
            }

            // Process!
            await _dataExporter.ExportAsync(request, cancelToken);

            Task OnProgress(int value, int max, string msg)
            {
                return ctx.SetProgressAsync(value, max, msg);
            }
        }
    }
}
