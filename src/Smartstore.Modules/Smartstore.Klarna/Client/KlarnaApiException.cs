using System;

namespace Smartstore.Klarna.Client
{
    public class KlarnaApiException : Exception
    {
        public KlarnaApiException(string message) : base(message)
        {
        }

        public KlarnaApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
