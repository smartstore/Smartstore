using System.Text;

namespace Smartstore.Pdf
{
    public abstract class PdfOptions
    {
        /// <summary>
        /// Processes the options by converting them to native arguments
        /// </summary>
        /// <param name="flag">The section flag</param>
        /// <param name="builder">The builder</param>
        /// <remarks>Possible flags are: page | header | footer | cover | toc</remarks>
        public abstract void Process(string flag, StringBuilder builder);
    }
}
