using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smartstore.IO;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class ImportExecuteContext
    {
        private readonly string _progressInfo;
        private DataExchangeAbortion _abortion;

        public ImportExecuteContext(string progressInfo, CancellationToken cancelToken)
        {
            _progressInfo = progressInfo;
            CancelToken = cancelToken;
        }

        public CancellationToken CancelToken { get; private set; }

        public DataImportRequest Request { get; internal set; }

        /// <summary>
        /// The data source (CSV, Excel etc.).
        /// </summary>
        public IDataTable DataTable { get; internal set; }

        /// <summary>
        /// Mapping information between database and data source.
        /// </summary>
        public ColumnMap ColumnMap { get; internal set; }

        public ImportDataSegmenter DataSegmenter { get; internal set; }

        /// <summary>
        /// A value indicating whether to only update existing records.
        /// </summary>
        public bool UpdateOnly { get; internal set; }

        /// <summary>
        /// Infos about the import file.
        /// </summary>
        public ImportFile File { get; internal set; }

        /// <summary>
        /// The import directory.
        /// </summary>
        public IDirectory ImportDirectory { get; internal set; }

        /// <summary>
        /// Logger instance to log information into the import log file.
        /// </summary>
        public ILogger Log { get; set; }

        /// <summary>
        /// Name of key fields to identify existing records for updating.
        /// </summary>
        public string[] KeyFieldNames { get; internal set; }

        /// <summary>
        /// Use this dictionary for any custom data required along the import.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// Gets or sets the result of the import.
        /// </summary>
        public ImportResult Result { get; set; }

        /// <summary>
        /// Extra import configuration data.
        /// </summary>
        public ImportExtraData ExtraData { get; internal set; }

        /// <summary>
        /// A value indicating whether and how to abort the import.
        /// </summary>
        public DataExchangeAbortion Abort
        {
            get
            {
                if (CancelToken.IsCancellationRequested || IsMaxFailures)
                    return DataExchangeAbortion.Hard;

                return _abortion;
            }
            set => _abortion = value;
        }

        public bool IsMaxFailures => Result.Errors > 11;

        public ProgressCallback ProgressCallback { get; internal set; }

        /// <summary>
        /// Allows to set a progress message.
        /// </summary>
        /// <param name="value">Progress value.</param>
        /// <param name="maximum">Progress maximum.</param>
        public async Task SetProgressAsync(int value, int maximum)
        {
            try
            {
                await ProgressCallback.Invoke(value, maximum, _progressInfo.FormatInvariant(value, maximum));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Allows to set a progress message.
        /// </summary>
        /// <param name="message">Output message.</param>
        public async Task SetProgressAsync(string message)
        {
            if (ProgressCallback != null && message.HasValue())
            {
                try
                {
                    await ProgressCallback.Invoke(0, 0, message);
                }
                catch
                {
                }
            }
        }
    }
}
