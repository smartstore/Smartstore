using System.Diagnostics;

namespace Smartstore.Pdf.WkHtml
{
    public class WkHtmlToPdfOptions
    {
        /// <summary>
        /// Get or set maximum execution time for PDF generation process (Default: null --> no timeout) 
        /// </summary>
        public TimeSpan? ExecutionTimeout { get; set; }

        /// <summary>
        /// Gets or sets wkhtmltopdf process priority. Default: Normal.
        /// </summary>
        public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.Normal;

        /// <summary>
        /// Get or set location for temp files (if not specified the current tenant temp directory is used for temp files) 
        /// </summary>
        /// <remarks>
        /// Temp files are used for providing cover page/header/footer HTML templates to wkhtmltopdf tool.
        /// </remarks>
        public string TempFilesPath { get; set; }

        /// <summary>
        /// Set this to your store's base url when the automatic url resolution fails (e.g. 'http://www.mystore.com')
        /// </summary>
        public Uri BaseUrl { get; set; }
    }
}
