#nullable enable

namespace Smartstore.PayPal.Client
{
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

        //public PayPalRequest WithRequestId(string payPalRequestId)
        //{
        //    Headers.Add("PayPal-Request-Id", payPalRequestId);
        //    return this;
        //}

        //public PayPalRequest WithPrefer(string prefer)
        //{
        //    Headers.Add("Prefer", prefer);
        //    return this;
        //}

        //public PayPalRequest WithClientMetadataId(string payPalClientMetadataId)
        //{
        //    Headers.Add("PayPal-Client-Metadata-Id", payPalClientMetadataId);
        //    return this;
        //}

        //public PayPalRequest WithBody(object body)
        //{
        //    Body = body;
        //    return this;
        //}

        public object Clone()
            => Clone<PayPalRequest>();

        public TRequest Clone<TRequest>() where TRequest : PayPalRequest
            => (TRequest)MemberwiseClone();

        public static string FormatPath(string path, params string[] tokens)
        {
            Guard.NotEmpty(path);

            if (tokens == null || tokens.Length == 0)
            {
                return path;
            }

            try
            {
                return path.FormatInvariant(tokens.Select(Uri.EscapeDataString).ToArray());
            }
            catch (IOException)
            {
                return path;
            }
        }
    }

    public class PayPalRequest<TMessage> : PayPalRequest
        where TMessage : class, new()
    {
        public PayPalRequest(string path, HttpMethod method)
            : base(path, method, typeof(TMessage))
        {
        }

        public new TMessage? Body { get; set; }
    }

    public class PayPalRequest<TRequest, TMessage> : PayPalRequest<TMessage>
        where TRequest : PayPalRequest<TRequest, TMessage>
        where TMessage : class, new()
    {
        public PayPalRequest(string path, HttpMethod method)
            : base(path, method)
        {
        }

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

        public TRequest WithClientMetadataId(string payPalClientMetadataId)
        {
            Headers.Add("PayPal-Client-Metadata-Id", payPalClientMetadataId);
            return (TRequest)this;
        }

        public TRequest WithBody(TMessage body)
        {
            Body = body;
            return (TRequest)this;
        }
    }
}
