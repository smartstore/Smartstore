using System.Globalization;

namespace Smartstore.AmazonPay.Services.Internal
{
    internal class AmazonPayData
    {
        public string MessageType { get; set; }
        public string MessageId { get; set; }
        public string AuthorizationId { get; set; }
        public string CaptureId { get; set; }
        public string RefundId { get; set; }
        public string ReferenceId { get; set; }

        public string ReasonCode { get; set; }
        public string ReasonDescription { get; set; }
        public string State { get; set; }
        public DateTime StateLastUpdate { get; set; }

        public AmazonPayAmount Fee { get; set; }
        public AmazonPayAmount AuthorizedAmount { get; set; }
        public AmazonPayAmount CapturedAmount { get; set; }
        public AmazonPayAmount RefundedAmount { get; set; }

        public bool? CaptureNow { get; set; }
        public DateTime Creation { get; set; }
        public DateTime? Expiration { get; set; }

        public string AnyAmazonId
        {
            get
            {
                if (CaptureId.HasValue())
                {
                    return CaptureId;
                }

                if (AuthorizationId.HasValue())
                {
                    return AuthorizationId;
                }

                return RefundId;
            }
        }

        public struct AmazonPayAmount
        {
            public AmazonPayAmount(decimal amount, string currencyCode)
            {
                Amount = amount;
                CurrencyCode = currencyCode;
            }

            public decimal Amount { get; }
            public string CurrencyCode { get; }

            public override string ToString()
            {
                var str = Amount.ToString("0.00", CultureInfo.InvariantCulture);
                return str.Grow(CurrencyCode, " ");
            }
        }
    }
}
