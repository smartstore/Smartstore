using Smartstore.StripeElements.Settings;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models;

namespace Smartstore.StripeElements.Models;

[LocalizedDisplay("Plugins.Smartstore.Stripe.")]
public class ConfigurationModel : ModelBase
{
    [LocalizedDisplay("*PublicApiKey")]
    public string PublicApiKey { get; set; }

    [LocalizedDisplay("*SecrectApiKey")]
    public string SecrectApiKey { get; set; }

    [LocalizedDisplay("*WebhookSecret")]
    public string WebhookSecret { get; set; }

    [LocalizedDisplay("*WebhookUrl")]
    public string WebhookUrl { get; set; }

    [LocalizedDisplay("*CaptureMethod")]
    public string CaptureMethod { get; set; }

    [LocalizedDisplay("*AdditionalFee")]
    public Amount AdditionalFee { get; set; } = new(unitField: nameof(StripeSettings.AdditionalFeePercentage));

    [LocalizedDisplay("*ShowButtonInMiniShoppingCart")]
    public bool ShowButtonInMiniShoppingCart { get; set; }

    [LocalizedDisplay("*PaymentPageButtonMethods")]
    public string[] PaymentPageButtonMethods { get; set; } = [];
}