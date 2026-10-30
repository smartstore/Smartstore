using Smartstore.ComponentModel;

namespace Smartstore.AmazonPay.Models;

/// <summary>
/// Specifies how the buyer entered the Amazon Pay checkout.
/// </summary>
public enum AmazonPayCheckoutFlow
{
    /// <summary>
    /// No checkout flow has been selected yet.
    /// </summary>
    None = 0,

    /// <summary>
    /// The buyer started the Amazon Pay express checkout, where Amazon provides the checkout data.
    /// </summary>
    Express,

    /// <summary>
    /// The buyer selected Amazon Pay on the payment method page.
    /// </summary>
    PaymentSelection
}

public class AmazonPayCheckoutState : ObservableObject
{
    /// <summary>
    /// The flow through which the buyer entered the Amazon Pay checkout.
    /// </summary>
    public AmazonPayCheckoutFlow Flow
    {
        get => GetProperty<AmazonPayCheckoutFlow>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The identifier of the Amazon Pay checkout session object.
    /// </summary>
    public string SessionId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    public override string ToString()
    {
        return $"Flow:{Flow}, SessionId:{SessionId}";
    }
}