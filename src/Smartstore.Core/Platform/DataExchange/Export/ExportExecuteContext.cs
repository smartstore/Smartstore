using Smartstore.Core.Localization;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Export
{
    public class ExportExecuteContext
    {
        private readonly DataExportResult _result;
        private readonly CancellationToken _cancelToken;
        private DataExchangeAbortion _providerAbort;

        internal ExportExecuteContext(DataExportResult result, CancellationToken cancelToken)
        {
            _result = result;
            _cancelToken = cancelToken;
        }

        /// <summary>
        /// Identifier of the export profile.
        /// </summary>
        public int ProfileId { get; internal set; }

        /// <summary>
        /// The export profile.
        /// </summary>
        public dynamic Profile { get; internal set; }

        /// <summary>
        /// Provides the data to be exported.
        /// </summary>
        public IExportDataSegmenterConsumer DataSegmenter { get; set; }

        /// <summary>
        /// The store context to be used for the export.
        /// </summary>
        public dynamic Store { get; internal set; }

        /// <summary>
        /// The customer context to be used for the export
        /// </summary>
        public dynamic Customer { get; internal set; }

        /// <summary>
        /// The currency context to be used for the export.
        /// </summary>
        public dynamic Currency { get; internal set; }

        /// <summary>
        /// The language context to be used for the export.
        /// </summary>
        public dynamic Language { get; internal set; }

        /// <summary>
        /// Filter settings.
        /// </summary>
        public ExportFilter Filter { get; internal set; }

        /// <summary>
        /// Projection settings.
        /// </summary>
        public ExportProjection Projection { get; internal set; }

        /// <summary>
        /// Logger to log information into the export log file.
        /// </summary>
        public ILogger Log { get; internal set; }

        /// <summary>
        /// Indicates whether and how to abort the export.
        /// </summary>
        public DataExchangeAbortion Abort
        {
            get
            {
                if (_cancelToken.IsCancellationRequested || IsMaxFailures)
                {
                    return DataExchangeAbortion.Hard;
                }

                return _providerAbort;
            }
            set => _providerAbort = value;
        }

        public bool IsMaxFailures => RecordsFailed > 11;

        /// <summary>
        /// Identifier of current data stream. Can be <c>null</c>.
        /// </summary>
        public string DataStreamId { get; set; }

        /// <summary>
        /// Stream used to write data to.
        /// </summary>
        public Stream DataStream { get; internal set; }

        /// <summary>
        /// List with extra data units/streams required by provider.
        /// </summary>
        public List<ExportDataUnit> ExtraDataUnits { get; private set; } = new();

        /// <summary>
        /// The maximum allowed file name length.
        /// </summary>
        public int MaxFileNameLength { get; internal set; }

        /// <summary>
        /// Index of current export file.
        /// </summary>
        public int FileIndex { get; internal set; }

        /// <summary>
        /// The name of the current export file.
        /// </summary>
        public string FileName { get; internal set; }

        /// <summary>
        /// The export directory.
        /// </summary>
        public IDirectory ExportDirectory { get; internal set; }

        /// <summary>
        /// A value indicating whether the profile has a public deployment into "Exchange" folder.
        /// </summary>
        public bool HasPublicDeployment { get; internal set; }

        /// <summary>
        /// The public export directory. In general, this is a subfolder of <see cref="DataExporter.PublicDirectoryName"/>.
        /// <c>null</c> if the profile has no public deployment.
        /// </summary>
        public IDirectory PublicDirectory { get; internal set; }

        /// <summary>
        /// The URL of the public export directory. <c>null</c> if the profile has no public deployment.
        /// </summary>
        public string PublicDirectoryUrl { get; internal set; }

        /// <summary>
        /// Provider specific configuration data.
        /// </summary>
        public object ConfigurationData { get; internal set; }

        /// <summary>
        /// Use this dictionary for any custom data required along the export.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// Number of successful processed records.
        /// </summary>
        public int RecordsSucceeded { get; set; }

        /// <summary>
        /// Number of failed records.
        /// </summary>
        public int RecordsFailed { get; set; }

        /// <summary>
        /// Processes an exception that occurred while exporting a record.
        /// </summary>
        /// <param name="entityId">Identifier of the current entity.</param>
        /// <param name="exception">Exception.</param>
        public void RecordException(Exception exception, int entityId)
        {
            ++RecordsFailed;

            Log.Error("Error while processing record with id {0}. {1}".FormatInvariant(entityId, exception.ToString()));

            if (IsMaxFailures)
                _result.LastError = exception.ToString();
        }

        /// <summary>
        /// Processes an out-of-memory exception and hard aborts the export.
        /// </summary>
        /// <param name="exception">Out-of-memory exception.</param>
        /// <param name="entityId">Identifier of the current entity.</param>
        /// <param name="localizer">Localizer.</param>
        public void RecordOutOfMemoryException(OutOfMemoryException exception, int entityId, Localizer localizer)
        {
            Abort = DataExchangeAbortion.Hard;

            var fileLength = Prettifier.HumanizeBytes(DataStream.Length);
            var batchSizeString = localizer("Admin.DataExchange.Export.BatchSize").Value;

            Log.Error($"No more memory could be allocated. Probably the export file is getting too large ({fileLength}). Please use profile setting \"{batchSizeString}\" to split the export into smaller files.");

            RecordException(exception, entityId);
        }

        public ProgressCallback ProgressCallback { get; internal set; }

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


    public class ExportDataUnit
    {
        /// <summary>
        /// Your Id to identify this stream within a list of streams.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Stream used to write data to.
        /// </summary>
        public Stream DataStream { get; internal set; }

        /// <summary>
        /// The related entity type.
        /// </summary>
        public RelatedEntityType? RelatedType { get; set; }

        /// <summary>
        /// The name of the file to be created.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Short optional text that describes the content of the file.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// A value indicating whether to display the file in the profile file dialog.
        /// </summary>
        public bool DisplayInFileDialog { get; set; }

        /// <summary>
        /// Number of successful processed records.
        /// </summary>
        public int RecordsSucceeded { get; set; }
    }
}
