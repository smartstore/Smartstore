using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.IO;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange.Import
{
    public partial class ImportExecuteContext
    {
        private readonly string _progressInfo;
        private DataExchangeAbortion _abortion;

        public ImportExecuteContext(string progressInfo, CancellationToken cancelToken = default)
        {
            _progressInfo = progressInfo;
            CancelToken = cancelToken;
        }

        /// <summary>
        /// A <see cref="CancellationToken"/> to observe while waiting for the task to complete.
        /// </summary>
        public CancellationToken CancelToken { get; private set; }

        public DataImportRequest Request { get; internal set; }

        public ImportEntityType ImportEntityType { get; internal set; }

        /// <summary>
        /// The data source (CSV, Excel etc.).
        /// </summary>
        public IDataTable DataTable { get; internal set; }

        /// <summary>
        /// Mapping information between database and data source.
        /// </summary>
        public ColumnMap ColumnMap { get; internal set; }

        /// <summary>
        /// The data segmenter that provides batched data to be imported.
        /// </summary>
        public IImportDataSegmenterConsumer DataSegmenter { get; internal set; }

        /// <summary>
        /// A value indicating whether to only update existing records.
        /// </summary>
        public bool UpdateOnly { get; internal set; }

        /// <summary>
        /// A value indicating whether to clear the entire cache at the end of the import.
        /// </summary>
        public bool ClearCache { get; internal set; }

        /// <summary>
        /// Infos about the import file.
        /// </summary>
        public ImportFile File { get; internal set; }

        /// <summary>
        /// Date and time at the beginning of the import.
        /// </summary>
        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// The import directory.
        /// E.g. App_Data\Tenants\Default\ImportProfiles\{my-import-profile}\Content.
        /// </summary>
        public IDirectory ImportDirectory { get; internal set; }

        /// <summary>
        /// The directory for downloading images during import.
        /// E.g. App_Data\Tenants\Default\ImportProfiles\{my-import-profile}\Content\DownloadedImage.
        /// If the import file contains URLs for images, they will be downloaded to this directory.
        /// </summary>
        public IDirectory ImageDownloadDirectory { get; internal set; }

        /// <summary>
        /// The directory with the images to be imported.
        /// E.g. App_Data\Tenants\Default\ImportProfiles\{my-import-profile}\my-images.
        /// If the import file contains relative paths for images, then they are expected to be in that directory.
        /// </summary>
        public IDirectory ImageDirectory { get; internal set; }

        /// <summary>
        /// Logger instance to log information into the import log file.
        /// </summary>
        public ILogger Log { get; set; }

        /// <summary>
        /// Name of key fields to identify existing records for updating.
        /// </summary>
        public string[] KeyFieldNames { get; internal set; }

        /// <summary>
        /// All active languages.
        /// </summary>
        public IReadOnlyCollection<Language> Languages { get; internal set; }

        /// <summary>
        /// All stores.
        /// </summary>
        public IReadOnlyCollection<Store> Stores { get; internal set; }

        /// <summary>
        /// Use this dictionary for any custom data required along the import.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// Gets or sets the result of the import.
        /// </summary>
        public ImportResult Result { get; set; } = new();

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
            if (message.HasValue())
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

        /// <summary>
        /// Gets a custom property. Creates it if it does not exist.
        /// </summary>
        /// <param name="key">Key\name of the custom property.</param>
        /// <returns>Custom property.</returns>
        public T GetCustomProperty<T>(string key)
        {
            if (!CustomProperties.ContainsKey(key))
            {
                CustomProperties[key] = Activator.CreateInstance(typeof(T));
            }

            return (T)CustomProperties[key];
        }
    }
}
