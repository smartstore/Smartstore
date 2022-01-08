using System.Net;
using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client
{
    public class PayPalResponse
    {
        private readonly object _result;

        public PayPalResponse(HttpHeaders headers, HttpStatusCode statusCode, object result)
        {
            Headers = headers;
            StatusCode = statusCode;
            _result = result;
        }

        public HttpHeaders Headers { get; }
        public HttpStatusCode StatusCode { get; }

        public T Result<T>()
            => (T)_result;
    }
}
