namespace Smartstore.Admin.Models.Orders;

/// <summary>
/// Contains the payment method rankings and their configured length.
/// </summary>
public class DashboardPaymentMethodsModel
{
    /// <summary>
    /// Gets or sets the maximum number of entries per ranking.
    /// </summary>
    public int Count { get; set; } = 10;

    /// <summary>
    /// Gets or sets the payment methods included in either ranking.
    /// </summary>
    public List<DashboardPaymentMethodModel> PaymentMethods { get; set; } = [];
}

/// <summary>
/// Contains language-independent order aggregates cached per authorized store selection.
/// </summary>
public class DashboardPaymentMethodTotals
{
    /// <summary>
    /// Gets or sets the payment provider identifier.
    /// </summary>
    public string SystemName { get; set; }

    /// <summary>
    /// Gets or sets the number of orders.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the order total across all payment statuses.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the order total for paid orders.
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Gets or sets the number of paid orders.
    /// </summary>
    public int PaidOrderCount { get; set; }
}
