using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.OfflinePayment.Components;
using Smartstore.OfflinePayment.Models;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    // TODO: (mh) (core) a masked payment summary on checkout confirm page is missing for form base offline methods.
    [SystemName("Payments.DirectDebit")]
    [FriendlyName("Direct Debit")]
    [Order(100)]
    public class DirectDebitProvider : OfflinePaymentProviderBase<DirectDebitPaymentSettings>, IConfigurable
    {
        private readonly IValidator<DirectDebitPaymentInfoModel> _validator;

        public DirectDebitProvider(
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            IValidator<DirectDebitPaymentInfoModel> validator)
            : base(storeContext, settingFactory)
        {
            _validator = validator;
        }

        protected override Type GetViewComponentType()
            => typeof(DirectDebitViewComponent);

        public RouteInfo GetConfigurationRoute()
            => new("DirectDebitConfigure", "OfflinePayment", new { area = "Admin" });

        public override async Task<PaymentValidationResult> ValidatePaymentDataAsync(IFormCollection form)
        {
            var model = new DirectDebitPaymentInfoModel
            {
                EnterIBAN = form["EnterIBAN"],
                DirectDebitAccountHolder = form["DirectDebitAccountHolder"],
                DirectDebitAccountNumber = form["DirectDebitAccountNumber"],
                DirectDebitBankCode = form["DirectDebitBankCode"],
                DirectDebitCountry = form["DirectDebitCountry"],
                DirectDebitBankName = form["DirectDebitBankName"],
                DirectDebitIban = form["DirectDebitIban"],
                DirectDebitBic = form["DirectDebitBic"]
            };

            var result = await _validator.ValidateAsync(model);
            return new PaymentValidationResult(result);
        }

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
            {
                DirectDebitAccountHolder = form["DirectDebitAccountHolder"],
                DirectDebitAccountNumber = form["DirectDebitAccountNumber"],
                DirectDebitBankCode = form["DirectDebitBankCode"],
                DirectDebitCountry = form["DirectDebitCountry"],
                DirectDebitBankName = form["DirectDebitBankName"],
                DirectDebitIban = form["DirectDebitIban"],
                DirectDebitBic = form["DirectDebitBic"]
            };

            return Task.FromResult(paymentInfo);
        }

        public override Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                AllowStoringDirectDebit = true,
                NewPaymentStatus = PaymentStatus.Pending
            };

            return Task.FromResult(result);
        }

        public override bool RequiresInteraction => true;
    }
}