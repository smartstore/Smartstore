using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client
{
    public class PayPalException : IOException
    {
        public PayPalException(PayPalResponse response, string message) 
            : base(message)
        {
            Response = Guard.NotNull(response, nameof(response));
        }

        public PayPalResponse Response { get; }
        public HttpStatusCode StatusCode { get => Response.StatusCode; }
        public HttpHeaders Headers { get => Response.Headers; }
    }
}
