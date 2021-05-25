using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Smartstore.Core.DataExchange.Import.Internal
{
    internal class DataImporterContext
    {
        public DataImportRequest Request { get; init; }
        public CancellationToken CancellationToken { get; init; }
        public ILogger Log { get; set; }
        public ImportExecuteContext ExecuteContext { get; init; }
        public IEntityImporter Importer { get; set; }
        public ColumnMap ColumnMap { get; init; }
        public Dictionary<string, ImportResult> Results { get; set; } = new();
    }
}
