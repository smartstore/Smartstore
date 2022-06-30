using System.Net;

namespace Smartstore.Core.Checkout.Payment
{
    public class PaymentResponse
    {
        private readonly object _body;

        public PaymentResponse(HttpStatusCode status)
            : this(status, null, null)
        {
        }

        public PaymentResponse(HttpStatusCode status, IDictionary<string, string> headers)
            : this(status, headers, null)
        {
        }

        public PaymentResponse(HttpStatusCode status, IDictionary<string, string> headers, object body)
        {
            Headers = headers;
            Status = status;
            _body = body;
        }

        public HttpStatusCode Status { get; }

        public IDictionary<string, string> Headers { get; }

        public bool HasBody
            => _body != null;

        public T Body<T>()
            => (T)_body;
    }
}
