using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;

namespace Smartstore.Web.Models.Checkout
{
    public class CheckoutConfirmMapper : Mapper<ShoppingCart, CheckoutConfirmModel>
    {
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CheckoutConfirmMapper(
            IStoreContext storeContext,
            IWorkContext workContext,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _storeContext = storeContext;
            _workContext = workContext;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(ShoppingCart from, CheckoutConfirmModel to, dynamic parameters = null)
        {
            Guard.NotNull(to);
            Guard.NotNull(from);

            to.TermsOfServiceEnabled = _orderSettings.TermsOfServiceEnabled;
            to.ShowEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox;
            to.NewsletterSubscription = _shoppingCartSettings.NewsletterSubscription;
            to.ThirdPartyEmailHandOver = _shoppingCartSettings.ThirdPartyEmailHandOver;

            if (_shoppingCartSettings.ThirdPartyEmailHandOver != CheckoutThirdPartyEmailHandOver.None)
            {
                to.ThirdPartyEmailHandOverLabel = _shoppingCartSettings.GetLocalizedSetting(
                    x => x.ThirdPartyEmailHandOverLabel,
                    _workContext.WorkingLanguage,
                    _storeContext.CurrentStore.Id,
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