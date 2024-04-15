using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.PayPal.Models
{
    [LocalizedDisplay("Plugins.Smartstore.PayPal.")]
    public class ConfigurationModel : ModelBase, ILocalizedModel<PayPalLocalizedModel>
    {
        public List<PayPalLocalizedModel> Locales { get; set; } = new();

        public bool HasCredentials { get; set; } = false;
        public bool PaymentsReceivable { get; set; } = false;
        public bool PrimaryEmailConfirmed { get; set; } = false;
        public bool WebHookCreated { get; set; } = false;
        public bool DisplayOnboarding { get; set; }

        [LocalizedDisplay("*UseSandbox")]
        public bool UseSandbox { get; set; }

        [LocalizedDisplay("*Account")]
        public string Account { get; set; }

        [LocalizedDisplay("*ClientId")]
        public string ClientId { get; set; }

        [LocalizedDisplay("*PayerId")]
        public string PayerId { get; set; }

        [LocalizedDisplay("*MerchantName")]
        public string MerchantName { get; set; }

        [LocalizedDisplay("*Secret")]
        public string Secret { get; set; }

        [LocalizedDisplay("*WebhookId")]
        public string WebhookId { get; set; }

        [LocalizedDisplay("*WebhookUrl")]
        public string WebhookUrl { get; set; }

        [LocalizedDisplay("*DisplayProductDetailPayLaterWidget")]
        public bool DisplayProductDetailPayLaterWidget { get; set; }

        [LocalizedDisplay("*CustomerServiceInstructions")]
        [UIHint("Textarea"), AdditionalMetadata("rows", 3)]
        public string CustomerServiceInstructions { get; set; }

        [LocalizedDisplay("*Intent")]
        public string Intent { get; set; } = "authorize";

        [LocalizedDisplay("*ButtonShape")]
        public string ButtonShape { get; set; } = "pill";

        [LocalizedDisplay("*ButtonColor")]
        public string ButtonColor { get; set; } = "gold";

        [LocalizedDisplay("*FundingsOffCanvasCart")]
        public string[] FundingsOffCanvasCart { get; set; }

        [LocalizedDisplay("*FundingsCart")]
        public string[] FundingsCart { get; set; }

        [LocalizedDisplay("*PayUponInvoiceLimit")]
        public decimal PayUponInvoiceLimit { get; set; }

        [LocalizedDisplay("*TransmitTrackingNumbers")]
        public bool TransmitTrackingNumbers { get; set; }
    }

    [LocalizedDisplay("Plugins.Smartstore.PayPal.")]
    public class PayPalLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*CustomerServiceInstructions")]
        [UIHint("Textarea"), AdditionalMetadata("rows", 3)]
        public string CustomerServiceInstructions { get; set; }
    }

    public partial class ConfigurationModelValidator : SettingModelValidator<ConfigurationModel, PayPalSettings>
    {
        public ConfigurationModelValidator(Localizer T)
        {
            RuleFor(x => x.MerchantName).NotEmpty();

            RuleFor(x => x.MerchantName)
                .Must(y => !y.Any(x => char.IsWhiteSpace(x)))
                .WithMessage(T("Plugins.Smartstore.PayPal.NoWhitespace"));

            RuleFor(x => x.CustomerServiceInstructions).NotEmpty();
        }
    }
}