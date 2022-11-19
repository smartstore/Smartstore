using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.Widgets;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    public abstract class OfflinePaymentProviderBase<TSetting> : PaymentMethodBase
        where TSetting : PaymentSettingsBase, ISettings, new()
    {
        public ICommonServices CommonServices { get; set; }

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        protected abstract string GetProviderName();

        protected abstract Type GetViewComponentType();

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await CommonServices.SettingFactory.LoadSettingsAsync<TSetting>(CommonServices.StoreContext.CurrentStore.Id);

            return new(settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(GetViewComponentType(), new { providerName = GetProviderName() });

        public override Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending
            };

            return Task.FromResult(result);
        }
    }
}
