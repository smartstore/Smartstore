using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Catalog.Products;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.DataExchange.Export
{
    public class DataExportRequest
    {
        private readonly static ProgressValueSetter _voidProgressValueSetter = SetProgress;

        public DataExportRequest(ExportProfile profile, Provider<IExportProvider> provider)
        {
            Guard.NotNull(profile, nameof(profile));
            Guard.NotNull(provider, nameof(provider));

            Profile = profile;
            Provider = provider;
            ProgressValueSetter = _voidProgressValueSetter;
        }

        public ExportProfile Profile { get; private set; }

        public Provider<IExportProvider> Provider { get; private set; }

        public ProgressValueSetter ProgressValueSetter { get; set; }

        public bool HasPermission { get; set; }

        public IList<int> EntitiesToExport { get; set; } = new List<int>();

        public string ActionOrigin { get; set; }

        public IDictionary<string, object> CustomData { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IQueryable<Product> ProductQuery { get; set; }

        private static void SetProgress(int val, int max, string msg)
        {
            // Do nothing.
        }
    }
}
