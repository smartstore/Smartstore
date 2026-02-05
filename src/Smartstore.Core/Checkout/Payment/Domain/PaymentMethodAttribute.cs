#nullable enable

namespace Smartstore.Core.Checkout.Payment;

/// <summary>
/// Specifies the type of payment method represented by a payment implementation class for use in payment processing systems.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PaymentMethodAttribute : Attribute
{
    public PaymentMethodAttribute(PaymentMethodType type)
    {
        Type = type;
    }

    public PaymentMethodType Type { get; }
}
