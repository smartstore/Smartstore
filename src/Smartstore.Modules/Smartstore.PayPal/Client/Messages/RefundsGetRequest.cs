using System;
using System.IO;
using System.Net.Http;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Shows details for a refund, by ID.
    /// </summary>
    public class RefundsGetRequest : PayPalRequest
    {
        public RefundsGetRequest(string refundId) 
            : base("/v2/payments/refunds/{0}?", HttpMethod.Get, typeof(RefundMessage))
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(refundId));
            }
            catch (IOException) 
            { 
            }

            ContentType = "application/json";
        }
    }
}
