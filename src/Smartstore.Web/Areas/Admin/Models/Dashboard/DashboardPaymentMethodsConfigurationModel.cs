using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Dashboard;

/// <summary>
/// Contains display settings and fee values for active payment methods.
/// </summary>
[LocalizedDisplay("Admin.Dashboard.PaymentMethods.")]
public class DashboardPaymentMethodsConfigurationModel
{
    /// <summary>
    /// Gets or sets the maximum number of payment methods per ranking.
    /// </summary>
    [LocalizedDisplay("Admin.Dashboard.Widget.Count")]
    public int Count { get; set; } = 10;

    /// <summary>
    /// Gets or sets the payment methods available for configuration.
    /// </summary>
    public List<DashboardPaymentMethodFeeModel> PaymentMethods { get; set; } = [];

    /// <summary>
    /// Gets or sets the primary currency label for fixed fees.
    /// </summary>
    public string CurrencyCode { get; set; }
}

/// <summary>
/// Contains the editable fee value and mode for one payment provider.
/// </summary>
[LocalizedDisplay("Admin.Dashboard.PaymentMethods.")]
public class DashboardPaymentMethodFeeModel
{
    /// <summary>
    /// Gets or sets the stable payment provider system name.
    /// </summary>
    public string SystemName { get; set; }

    /// <summary>
    /// Gets or sets the localized provider name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the fee value in percent or the primary currency.</summary>
    [LocalizedDisplay("*Fee")]
    public decimal FeeValue { get; set; }

    /// <summary>Gets or sets whether the fee value is a percentage.</summary>
    [LocalizedDisplay("*FeePercentage")]
    public bool FeeIsPercentage { get; set; } = true;
}
