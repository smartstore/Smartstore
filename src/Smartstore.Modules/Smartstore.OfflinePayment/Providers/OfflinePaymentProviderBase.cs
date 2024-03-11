using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.OfflinePayment.Settings;

namespace Smartstore.OfflinePayment
{
    public abstract class OfflinePaymentProviderBase<TSetting> : PaymentMethodBase
        where TSetting : PaymentSettingsBase, ISettings, new()
    {
        protected readonly IStoreContext _storeContext;
        protected readonly ISettingFactory _settingFactory;

        protected OfflinePaymentProviderBase(
            IStoreContext storeContext, 
            ISettingFactory settingFactory)
        {
            _storeContext = storeContext;
            _settingFactory = settingFactory;
        }

        public override bool RequiresPaymentSelection => false;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        protected virtual Type GetViewComponentType()
            => null;

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _settingFactory.LoadSettingsAsync<TSetting>(_storeContext.CurrentStore.Id);
            return new(settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        public override Widget GetPaymentInfoWidget()
        {
            var type = GetViewComponentType();
            return type != null ? new ComponentWidget(type) : null;
        }

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
