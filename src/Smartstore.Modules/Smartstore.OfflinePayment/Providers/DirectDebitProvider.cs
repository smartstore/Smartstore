using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Payment;
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
    [Order(1)]
    public class DirectDebitProvider : OfflinePaymentProviderBase<DirectDebitPaymentSettings>, IConfigurable
    {
        protected override Type GetViewComponentType()
        {
            return typeof(DirectDebitViewComponent);
        }

        // TODO: (not needed here make optional)
        protected override string GetProviderName()
        {
            return nameof(DirectDebitProvider);
        }
        public RouteInfo GetConfigurationRoute()
            => new("DirectDebitConfigure", "OfflinePayment", new { area = "Admin" });

        public override Task<List<string>> GetPaymentDataWarningsAsync(IFormCollection form)
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

            var validator = new DirectDebitPaymentInfoValidator();
            var result = validator.Validate(model);

            if (result != null && !result.IsValid)
            {
                var errors = result.Errors
                    .Select(x => x.ErrorMessage)
                    .ToList();

                return Task.FromResult(errors);
            }

            return base.GetPaymentDataWarningsAsync(form);
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