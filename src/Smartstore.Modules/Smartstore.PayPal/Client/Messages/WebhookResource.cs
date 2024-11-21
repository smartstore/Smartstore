using System.ComponentModel.DataAnnotations;

namespace Smartstore.PayPal.Client.Messages
{
    public class WebhookResource
    {
        /// <summary>
        /// The API caller-provided external ID. Used to reconcile API caller-initiated transactions with PayPal transactions. Appears in transaction and settlement reports.
        /// </summary>
        public string CustomId { get; set; }

        /// <summary>
        /// Contextaware transaction id (AuthorizationId, CaputureId or RefundeId). 
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Contextaware status message. 
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Transaction amount.
        /// </summary>
        public MoneyMessage Amount;

        /// <summary>
        /// An array of purchase units. Each purchase unit establishes a contract between a payer and the payee.
        /// </summary>
        [Required]
        public PurchaseUnit[] PurchaseUnits;
    }
}
