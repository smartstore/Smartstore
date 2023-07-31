using Smartstore.Core.Configuration;

namespace Smartstore.Core.DataExchange
{
    public class DataExchangeSettings : ISettings
    {
        /// <summary>
        /// The maximum length of file names (in characters) of files created by the export framework.
        /// </summary>
        public int MaxFileNameLength { get; set; } = 50;

        #region Import

        /// <summary>
        /// Relative path to a folder with images to be imported.
        /// </summary>
        public string ImageImportFolder { get; set; }

        /// <summary>
        /// The timeout for image download per entity in minutes.
        /// </summary>
        public int ImageDownloadTimeout { get; set; } = 10;

        /// <summary>
        /// Specifies whether to send an email when an import has completed.
        /// </summary>
        public DataExchangeCompletionEmail ImportCompletionEmail { get; set; } = DataExchangeCompletionEmail.Always;

        #endregion
    }
}
