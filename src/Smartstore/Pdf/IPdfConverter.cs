using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        Task<byte[]> ConvertAsync(PdfConvertSettings settings);
    }

    internal class NullPdfConverter : IPdfConverter
    {
        public Task<byte[]> ConvertAsync(PdfConvertSettings settings)
        {
            throw new NotSupportedException();
        }
    }
}
