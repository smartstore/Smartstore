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
}
