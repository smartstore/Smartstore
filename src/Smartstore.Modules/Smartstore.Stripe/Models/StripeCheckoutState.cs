using Smartstore.ComponentModel;

namespace Smartstore.StripeElements.Models;

public class StripeCheckoutState : ObservableObject
{
    // INFO: Uncommented because this object can be serialized due to https://github.com/stripe/stripe-dotnet/pull/3254
    // TODO: Use again once this issue is resolved.
    //public PaymentIntent PaymentIntent
    //{
    //    get => GetProperty<PaymentIntent>();
    //    set => SetProperty(value);
    //}

    public string PaymentIntentId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    public bool ButtonUsed
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    public string PaymentMethod
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    #region Confirmation flow

    public bool IsConfirmed
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    public string FormData
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Order is confimed by buyer and Stripe -> automatically submit confirm form.
    /// </summary>
    public bool SubmitForm
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    #endregion

    public override string ToString()
    {
        return $"PaymentIntentId:{PaymentIntentId}";
    }
}
