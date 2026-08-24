#nullable enable

namespace Smartstore.Core.Checkout.Orders;

/// <summary>
/// Represents the data required to recover an order.
/// </summary>
public partial class OrderRecoveryData
{
    /// <summary>
    /// Gets or sets the store identifier. Must not be 0.
    /// </summary>
    public int StoreId { get; init; }

    /// <summary>
    /// Gets or sets the customer identifier. Must not be 0.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets or sets an order GUID. Applied to the order to be recovered.
    /// If not provided, a new order GUID will be generated.
    /// </summary>
    public Guid OrderGuid { get; init; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the system name of the payment provider used to pay the order.
    /// </summary>
    public required string PaymentMethodSystemName { get; init; }

    /// <summary>
    /// Gets or sets the amount paid for the order.
    /// Must match the total amount of the current shopping cart of the customer.
    /// </summary>
    public decimal PaidAmount { get; init; }

    /// <summary>
    /// Gets or sets the hash of the original, paid shopping cart.
    /// Must match the hash of the current shopping cart of the customer.
    /// </summary>
    public int CartHash { get; init; }
}
