using Smartstore.ComponentModel;

namespace Smartstore.AmazonPay.Models;

[Serializable]
public class AmazonPayCheckoutState : ObservableObject
{
    /// <summary>
    /// The identifier of the AmazonPay checkout session object.
    /// </summary>
    public string SessionId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    public override string ToString()
    {
        return $"SessionId:{SessionId}";
    }
}
