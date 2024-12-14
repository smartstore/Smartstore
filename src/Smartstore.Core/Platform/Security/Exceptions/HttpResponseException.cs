namespace Smartstore.Core.Security
{
    public class HttpResponseException(int statusCode, string message = null) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
    }
}
