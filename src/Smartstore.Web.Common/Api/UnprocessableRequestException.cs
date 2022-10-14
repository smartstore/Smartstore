using System.Net;

namespace Smartstore.Web.Api
{
    /// <summary>
    /// Thrown when a Web API request cannot be processed.
    /// </summary>
    [Serializable]
    public class UnprocessableRequestException : SmartException
    {
        public UnprocessableRequestException(
            string message,
            HttpStatusCode statusCode = HttpStatusCode.UnprocessableEntity)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public UnprocessableRequestException(
            string message,
            Exception innerException,
            HttpStatusCode statusCode = HttpStatusCode.UnprocessableEntity)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the <see cref="HttpStatusCode"/> to be returned.
        /// </summary>
        public HttpStatusCode StatusCode { get; }
    }
}
