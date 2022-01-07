using Smartstore.Core.Catalog.Products;
using Smartstore.Engine.Modularity;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Export
{
    public class DataExportRequest
    {
        public DataExportRequest(ExportProfile profile, Provider<IExportProvider> provider)
        {
            Guard.NotNull(profile, nameof(profile));
            Guard.NotNull(provider, nameof(provider));

            Profile = profile;
            Provider = provider;
            ProgressCallback = OnProgress;
        }

        public ExportProfile Profile { get; private set; }

        public Provider<IExportProvider> Provider { get; private set; }

        public ProgressCallback ProgressCallback { get; init; }

        public bool HasPermission { get; set; }

        public IList<int> EntitiesToExport { get; set; } = new List<int>();

        public string ActionOrigin { get; set; }

        public IDictionary<string, object> CustomData { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IQueryable<Product> ProductQuery { get; set; }

        Task OnProgress(int value, int max, string msg)
        {
            return Task.CompletedTask;
        }
    }
}
