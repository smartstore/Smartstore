#nullable enable

namespace Smartstore.PayPal.Client
{
    public class PayPalRequest<TMessage> : PayPalRequest
    {
        public PayPalRequest(string path, HttpMethod method)
            : base(path, method, typeof(TMessage))
        {
        }
    }

    public class PayPalRequest2<TRequest, TMessage> : PayPalRequest
        where TRequest : PayPalRequest2<TRequest, TMessage>
        where TMessage : class, new()
    {
        public PayPalRequest2(string path, HttpMethod method)
            : base(path, method, typeof(TMessage))
        {
        }

        public new TMessage? Body { get; set; }

        public TRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return (TRequest)this;
        }

        public TRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return (TRequest)this;
        }

        public TRequest WithBody(TMessage body)
        {
            Body = body;
            return (TRequest)this;
        }
    }

    public class PayPalRequest : HttpRequestMessage, ICloneable
    {
        const string DefaultContentType = "application/json";
        const string DefaultContentEncoding = "identity";

        public PayPalRequest(string path, HttpMethod method)
            : this(path, method, typeof(void))
        {
        }

        public PayPalRequest(string path, HttpMethod method, Type responseType)
        {
            Guard.NotEmpty(path);

            Path = path;
            Method = method;
            ResponseType = Guard.NotNull(responseType);
        }

        public string Path { get; set; }
        public object? Body { get; set; }
        public Type ResponseType { get; }
        public string ContentType { get; set; } = DefaultContentType;
        public string ContentEncoding { get; set; } = DefaultContentEncoding;

        public object Clone()
            => Clone<PayPalRequest>();

        public TRequest Clone<TRequest>() where TRequest : PayPalRequest
            => (TRequest)MemberwiseClone();
    }
}
