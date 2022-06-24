namespace Smartstore.Pdf
{
    public interface IPdfConverter
    {
        /// <summary>
        /// Creates a provider specific URL or local physical path input type <see cref="IPdfInput"/>.
        /// </summary>
        /// <param name="urlOrPath">The input URL (relative or absolute) or a physical file path.</param>
        /// <param name="prefetch">Whether to download <paramref name="urlOrPath"/> before processing starts. Does not apply to local physical paths.</param>
        IPdfInput CreateFileInput(string urlOrPath, bool prefetch = false);

        /// <summary>
        /// Creates a provider specific HTML content input type <see cref="IPdfInput"/>.
        /// </summary>
        /// <param name="html">The input HTML</param>
        IPdfInput CreateHtmlInput(string html);

        /// <summary>
        /// Converts html content to PDF
        /// </summary>
        /// <param name="settings">The settings to be used for the conversion process</param>
        /// <returns>The resulting PDF as a temporary file stream. The temporary file is deleted on stream close.</returns>
        Task<Stream> GeneratePdfAsync(PdfConversionSettings settings, CancellationToken cancelToken = default);

        ///// <summary>
        ///// Converts html content to PDF
        ///// </summary>
        ///// <param name="settings">The settings to be used for the conversion process</param>
        ///// <param name="output">The stream to write the PDF output to.</param>
        //Task ConvertAsync(PdfConversionSettings settings, Stream output);
    }

    public static class IPdfConverterExtensions
    {
        /// <summary>
        /// Converts html content to PDF
        /// </summary>
        /// <param name="settings">The settings to be used for the conversion process</param>
        /// <param name="output">The stream to write the PDF output to.</param>
        public static async Task GeneratePdfAsync(this IPdfConverter converter, PdfConversionSettings settings, Stream output, CancellationToken cancelToken = default)
        {
            Guard.NotNull(converter, nameof(converter));
            Guard.NotNull(output, nameof(output));

            using (var outStream = await converter.GeneratePdfAsync(settings, cancelToken))
            {
                await outStream.CopyToAsync(output, cancelToken);
            }
        }
    }

    internal class NullPdfConverter : IPdfConverter
    {
        public IPdfInput CreateFileInput(string urlOrPath, bool prefetch = false)
            => null;

        public IPdfInput CreateHtmlInput(string html)
            => null;

        public Task<Stream> GeneratePdfAsync(PdfConversionSettings settings, CancellationToken cancelToken = default)
            => Task.FromResult<Stream>(null);
    }
}
