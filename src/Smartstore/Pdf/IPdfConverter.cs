using System;
using System.Threading.Tasks;

namespace Smartstore.Pdf
{
    public interface IPdfConverter
    {
        // TODO: (core) Implement PdfConverter (DinkToPdf)
        // TODO: (core) Find a way to (externally) package and deploy native libraries (e.g. with https://github.com/olegtarasov/NativeLibraryManager)

        /// <summary>
        /// Converts html content to PDF
        /// </summary>
        /// <param name="settings">The settings to be used for the conversion process</param>
        /// <returns>The PDF binary data</returns>
        Task<byte[]> ConvertAsync(PdfConversionSettings settings);
    }

    internal class NullPdfConverter : IPdfConverter
    {
        public Task<byte[]> ConvertAsync(PdfConversionSettings settings)
        {
            throw new NotSupportedException();
        }
    }
}
