#nullable enable

namespace Smartstore.Core.Checkout.Payment;

/// <summary>
/// Declares the payment method type supported by a payment provider.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PaymentMethodTypeAttribute : Attribute
{
    public PaymentMethodType PaymentMethodType { get; set; }
}
