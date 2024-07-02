using System.Net;

namespace Smartstore.Core.Checkout.Payment
{
    public class PaymentResponse(HttpStatusCode status, IDictionary<string, string> headers, object body)
    {
        private readonly object _body = body;

        public PaymentResponse(HttpStatusCode status)
            : this(status, null, null)
        {
        }

        public PaymentResponse(HttpStatusCode status, IDictionary<string, string> headers)
            : this(status, headers, null)
        {
        }

        public HttpStatusCode Status { get; } = status;

        public IDictionary<string, string> Headers { get; } = headers;

        public bool HasBody
            => _body != null;

        public T Body<T>()
            => (T)_body;
    }
}
