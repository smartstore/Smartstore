using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static Task MapAsync(this ShoppingCart cart, CheckoutConfirmModel model)
        {
            return MapperFactory.MapAsync(cart, model, null);
        }
    }

    public class CheckoutConfirmMapper : Mapper<ShoppingCart, CheckoutConfirmModel>
    {
        private readonly ICommonServices _services;
        private readonly OrderSettings _orderSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CheckoutConfirmMapper(
            ICommonServices services,
            OrderSettings orderSettings,
            PaymentSettings paymentSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _services = services;
            _orderSettings = orderSettings;
            _paymentSettings = paymentSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(ShoppingCart from, CheckoutConfirmModel to, dynamic parameters = null)
        {
            Guard.NotNull(to, nameof(to));
            Guard.NotNull(from, nameof(from));

            to.TermsOfServiceEnabled = _orderSettings.TermsOfServiceEnabled;
            to.ShowEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox;
            to.BypassPaymentMethodInfo = _paymentSettings.BypassPaymentMethodInfo;
            to.NewsletterSubscription = _shoppingCartSettings.NewsletterSubscription;
            to.ThirdPartyEmailHandOver = _shoppingCartSettings.ThirdPartyEmailHandOver;

            if (_shoppingCartSettings.ThirdPartyEmailHandOver != CheckoutThirdPartyEmailHandOver.None)
            {
                to.ThirdPartyEmailHandOverLabel = _shoppingCartSettings.GetLocalizedSetting(
                    x => x.ThirdPartyEmailHandOverLabel,
                    _services.WorkContext.WorkingLanguage,
                    _services.StoreContext.CurrentStore.Id,
                    true,
                    false);

                if (to.ThirdPartyEmailHandOverLabel.IsEmpty())
                {
                    to.ThirdPartyEmailHandOverLabel = T("Admin.Configuration.Settings.ShoppingCart.ThirdPartyEmailHandOverLabel.Default");
                }
            }
        }
    }
}