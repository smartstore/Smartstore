using System;
using System.IO;
using System.Net.Http;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Captures an authorized order, by ID.
    /// </summary>
    public class AuthorizationsCaptureRequest : PayPalRequest<CaptureMessage>
    {
        public AuthorizationsCaptureRequest(string captureId, int storeId)
            : base("/v2/payments/authorizations/{0}/capture?", HttpMethod.Post)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(captureId));
                StoreId = storeId;
            }
            catch (IOException)
            {
            }

            ContentType = "application/json";
        }

        public AuthorizationsCaptureRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }

        public AuthorizationsCaptureRequest WithPrefer(string prefer)
        {
            Headers.Add("Prefer", prefer);
            return this;
        }

        public AuthorizationsCaptureRequest WithBody(CaptureMessage refundRequest)
        {
            Body = refundRequest;
            return this;
        }
    }
}
