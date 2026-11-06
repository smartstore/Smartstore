namespace Smartstore.Admin.Models.Orders;

/// <summary>
/// Represents one payment method in the dashboard ranking.
/// </summary>
[LocalizedDisplay("Admin.Dashboard.PaymentMethods.")]
public class DashboardPaymentMethodModel : ModelBase
{
    /// <summary>
    /// Gets or sets the localized payment method name or its system name when unavailable.
    /// </summary>
    [LocalizedDisplay("*Name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the number of orders using this payment method.
    /// </summary>
    [LocalizedDisplay("*OrderCount")]
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the sum of order totals, including tax and shipping, in the primary currency.
    /// </summary>
    [LocalizedDisplay("Common.Amount")]
    public Money TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the configured fee value, or null when no fee has been configured.
    /// </summary>
    [LocalizedDisplay("*Fee")]
    public decimal? FeeValue { get; set; }

    /// <summary>
    /// Gets or sets estimated fees on paid order totals in the primary currency.
    /// </summary>
    [LocalizedDisplay("*EstimatedFees")]
    public Money EstimatedFees { get; set; }

    /// <summary>
    /// Gets or sets whether the configured fee is a percentage.
    /// </summary>
    [LocalizedDisplay("*FeePercentage")]
    public bool FeeIsPercentage { get; set; }

}
