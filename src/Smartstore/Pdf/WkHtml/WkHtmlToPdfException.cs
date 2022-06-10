namespace Smartstore.Pdf.WkHtml
{
    public sealed class WkHtmlToPdfException : Exception
    {
        public WkHtmlToPdfException(int errCode, string message) : base($"{message} (exit code: {errCode})")
        {
            ErrorCode = errCode;
        }

        public int ErrorCode { get; }
    }
}
