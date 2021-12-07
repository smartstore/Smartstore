using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.OfflinePayment.Models
{
    public abstract class PaymentInfoModelBase : ModelBase
    {
        public string DescriptionText { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    public class CashOnDeliveryPaymentInfoModel : PaymentInfoModelBase
    {
    }

    [LocalizedDisplay("Plugins.Payments.DirectDebit.")]
    public class DirectDebitPaymentInfoModel : PaymentInfoModelBase
    {
        [LocalizedDisplay("*EnterIBAN")]
        public string EnterIBAN { get; set; } = "iban";

        [LocalizedDisplay("*DirectDebitAccountHolder")]
        public string DirectDebitAccountHolder { get; set; }

        [LocalizedDisplay("*DirectDebitAccountNumber")]
        public string DirectDebitAccountNumber { get; set; }

        [LocalizedDisplay("*DirectDebitBankCode")]
        public string DirectDebitBankCode { get; set; }

        [LocalizedDisplay("*DirectDebitCountry")]
        public string DirectDebitCountry { get; set; }

        [LocalizedDisplay("*DirectDebitBankName")]
        public string DirectDebitBankName { get; set; }

        [LocalizedDisplay("*DirectDebitIban")]
        public string DirectDebitIban { get; set; }

        [LocalizedDisplay("*DirectDebitBic")]
        public string DirectDebitBic { get; set; }

        public List<SelectListItem> Countries { get; set; } = new();
    }

    public class InvoicePaymentInfoModel : PaymentInfoModelBase
    {
    }

    [LocalizedDisplay("Payment.")]
    public class ManualPaymentInfoModel : PaymentInfoModelBase
    {
        [LocalizedDisplay("*SelectCreditCard")]
        public string CreditCardType { get; set; }
        [LocalizedDisplay("*SelectCreditCard")]
        public List<SelectListItem> CreditCardTypes { get; set; } = new();

        [LocalizedDisplay("*CardholderName")]
        public string CardholderName { get; set; }

        [LocalizedDisplay("*CardNumber")]
        public string CardNumber { get; set; }

        [LocalizedDisplay("*ExpirationDate")]
        public string ExpireMonth { get; set; }
        [LocalizedDisplay("*ExpirationDate")]
        public string ExpireYear { get; set; }
        public List<SelectListItem> ExpireMonths { get; set; } = new();
        public List<SelectListItem> ExpireYears { get; set; } = new();

        [LocalizedDisplay("*CardCode")]
        public string CardCode { get; set; }
    }

    public class PayInStorePaymentInfoModel : PaymentInfoModelBase
    {
    }

    public class PrepaymentPaymentInfoModel : PaymentInfoModelBase
    {
    }

    public class PurchaseOrderNumberPaymentInfoModel : PaymentInfoModelBase
    {
        [LocalizedDisplay("Plugins.Payments.PurchaseOrder.PurchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; }
    }


    #region Validators 

    public class DirectDebitPaymentInfoValidator : AbstractValidator<DirectDebitPaymentInfoModel>
    {
        public DirectDebitPaymentInfoValidator()
        {
            RuleFor(x => x.DirectDebitAccountHolder).NotEmpty();
            RuleFor(x => x.DirectDebitAccountNumber).NotEmpty().When(x => x.EnterIBAN == "no-iban");
            RuleFor(x => x.DirectDebitBankCode).NotEmpty().When(x => x.EnterIBAN == "no-iban");
            RuleFor(x => x.DirectDebitIban).Matches(RegularExpressions.IsIban).When(x => x.EnterIBAN == "iban");
            RuleFor(x => x.DirectDebitBic).Matches(RegularExpressions.IsBic).When(x => x.EnterIBAN == "iban");
        }
    }

    public class ManualPaymentInfoValidator : AbstractValidator<ManualPaymentInfoModel>
    {
        public ManualPaymentInfoValidator(Localizer T)
        {
            RuleFor(x => x.CardholderName).NotEmpty();
            RuleFor(x => x.CardNumber).CreditCard().WithMessage(T("Payment.CardNumber.Wrong"));
            RuleFor(x => x.CardCode).CreditCardCvvNumber();
        }
    }

    public class PurchaseOrderNumberPaymentInfoValidator : AbstractValidator<PurchaseOrderNumberPaymentInfoModel>
    {
        public PurchaseOrderNumberPaymentInfoValidator()
        {
            RuleFor(x => x.PurchaseOrderNumber).NotEmpty();
        }
    }

    #endregion
}
