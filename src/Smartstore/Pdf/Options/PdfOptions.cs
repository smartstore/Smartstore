using DinkToPdf;

namespace Smartstore.Pdf
{
    public abstract class PdfOptions
    {
        /// <summary>
        /// Processes the options by converting them to library specific settings.
        /// </summary>
        /// <param name="flag">The section flag</param>
        /// <param name="document">The library document instance</param>
        /// <remarks>Possible flags are: page | header | footer | cover | toc</remarks>
        protected internal abstract void Apply(string flag, HtmlToPdfDocument document);
    }
}
