using System.Net;
using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client
{
    public class PayPalResponse
    {
        private readonly object _message;

        public PayPalResponse(HttpHeaders headers, HttpStatusCode statusCode, object message)
        {
            Headers = headers;
            StatusCode = statusCode;
            _message = message;
        }

        public HttpHeaders Headers { get; }
        public HttpStatusCode StatusCode { get; }

        public T Message<T>()
            => (T)_message;
    }
}
