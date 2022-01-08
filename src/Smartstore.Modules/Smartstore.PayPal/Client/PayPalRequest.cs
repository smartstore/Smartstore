using System;
using System.Net.Http;

namespace Smartstore.PayPal.Client
{
    public class PayPalRequest : HttpRequestMessage
    {
        public PayPalRequest(string path, HttpMethod method, Type responseType)
        {
            Path = path;
            ResponseType = responseType;
            Method = method;
            ContentEncoding = "identity";
        }

        public PayPalRequest(string path, HttpMethod method) 
            : this(path, method, typeof(void)) 
        { 
        }

        public string Path { get; set; }
        public object Body { get; set; }
        public string ContentType { get; set; }
        public string ContentEncoding { get; set; }
        public Type ResponseType { get; }

        public T Clone<T>() where T : PayPalRequest
            => (T)MemberwiseClone();
    }
}
