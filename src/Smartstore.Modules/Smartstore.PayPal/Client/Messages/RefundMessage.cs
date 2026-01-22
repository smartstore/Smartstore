namespace Smartstore.PayPal.Client.Messages;

/// <summary>
/// The refund information.
/// </summary>
public class RefundMessage
{
    /// <summary>
    /// The currency and amount for a financial transaction, such as a balance or payment due.
    /// </summary>
    public MoneyMessage Amount;

    /// <summary>
    /// The date and time, in [Internet date and time format](https://tools.ietf.org/html/rfc3339#section-5.6). 
    /// Seconds are required while fractional seconds are optional.<blockquote><strong>Note:</strong> 
    /// The regular expression provides guidance but does not reject all invalid dates.</blockquote>
    /// </summary>
    public string CreateTime;

    /// <summary>
    /// The PayPal-generated ID for the refund.
    /// </summary>
    public string Id;

    /// <summary>
    /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
    /// </summary>
    public string InvoiceId;

    ///// <summary>
    ///// An array of related [HATEOAS links](/docs/api/overview/#hateoas-links).
    ///// </summary>
    //public List<LinkDescription> Links;

    /// <summary>
    /// The reason for the refund. Appears in both the payer's transaction history and the emails that the payer receives.
    /// </summary>
    public string NoteToPayer;

    ///// <summary>
    ///// The breakdown of the refund.
    ///// </summary>
    //public MerchantPayableBreakdown SellerPayableBreakdown;

    /// <summary>
    /// The status of the capture.
    /// </summary>
    public string Status;

    ///// <summary>
    ///// The details of the refund status.
    ///// </summary>
    //public StatusDetails StatusDetails;

    /// <summary>
    /// The date and time, in [Internet date and time format](https://tools.ietf.org/html/rfc3339#section-5.6). Seconds are required while fractional seconds are optional.<blockquote><strong>Note:</strong> The regular expression provides guidance but does not reject all invalid dates.</blockquote>
    /// </summary>
    public string UpdateTime;
}
