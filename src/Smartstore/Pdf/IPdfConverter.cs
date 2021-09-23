using System;
using System.IO;
using System.Threading.Tasks;

namespace Smartstore.Pdf
{
    public interface IPdfConverter
    {
        /// <summary>
        /// Creates a provider specific URL type <see cref="IPdfInput"/>.
        /// </summary>
        /// <param name="url">The input URL</param>
        IPdfInput CreateUrlInput(string url);

        /// <summary>
        /// Creates a provider specific HTML content type <see cref="IPdfInput"/>.
        /// </summary>
        /// <param name="html">The input HTML</param>
        IPdfInput CreateHtmlInput(string html);

        /// <summary>
        /// Converts html content to PDF
        /// </summary>
        /// <param name="settings">The settings to be used for the conversion process</param>
        /// <param name="output">The stream to write the PDF output to.</param>
        Task ConvertAsync(PdfConversionSettings settings, Stream output);
    }

    internal class NullPdfConverter : IPdfConverter
    {
        public Task ConvertAsync(PdfConversionSettings settings, Stream output)
            => throw new NotImplementedException();

        public IPdfInput CreateHtmlInput(string html)
            => null;

        public IPdfInput CreateUrlInput(string url)
            => null;
    }
}
