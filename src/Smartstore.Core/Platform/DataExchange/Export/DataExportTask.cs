using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
                .Include(x => x.Task) // TODO: (mg) (core) You probably include an entity which already is loaded (ctx.ExecutionInfo.Task)
                .Include(x => x.Deployments)
                .FindByIdAsync(profileId, true, cancelToken);

            if (profile == null)
            {
                return;
            }

            var provider = _providerManager.GetProvider<IExportProvider>(profile.ProviderSystemName);
            if (provider == null)
            {
                throw new SmartException(T("Admin.Common.ProviderNotLoaded", profile.ProviderSystemName.NaIfEmpty()));
            }

            // Create export request.
            var request = new DataExportRequest(profile, provider)
            {
                ProgressValueSetter = delegate (int val, int max, string msg)
                {
                    // TODO: (mg) (core) No way!! The delegate must follow the signature and has to be of type Task, not void!
                    ctx.SetProgressAsync(val, max, msg, true);
                }
            };

            if (ctx.Parameters.ContainsKey("SelectedIds"))
            {
                request.EntitiesToExport = ctx.Parameters["SelectedIds"]
                    .SplitSafe(",")
                    .Select(x => x.ToInt())
                    .ToList();
            }

            if (ctx.Parameters.ContainsKey("ActionOrigin"))
            {
                request.ActionOrigin = ctx.Parameters["ActionOrigin"];
            }

            // Process!
            await _dataExporter.ExportAsync(request, cancelToken);
        }
    }
}
