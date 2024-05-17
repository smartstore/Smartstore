using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Models.Checkout
{
    public class CheckoutConfirmMapper : Mapper<CheckoutContext, CheckoutConfirmModel>
    {
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly ICheckoutFactory _checkoutFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CheckoutConfirmMapper(
            IStoreContext storeContext,
            IWorkContext workContext,
            ICheckoutFactory checkoutFactory,
            IShoppingCartService shoppingCartService,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _storeContext = storeContext;
            _workContext = workContext;
            _checkoutFactory = checkoutFactory;
            _shoppingCartService = shoppingCartService;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(CheckoutContext from, CheckoutConfirmModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(CheckoutContext from, CheckoutConfirmModel to, dynamic parameters = null)
        {
            Guard.NotNull(to);
            Guard.NotNull(from);

            var storeId = _storeContext.CurrentStore.Id;
            var cart = await _shoppingCartService.GetCartAsync(null, ShoppingCartType.ShoppingCart, storeId);

            to.ActionName = CheckoutActionNames.Confirm;
            to.PreviousStepUrl = _checkoutFactory.GetNextCheckoutStepUrl(from, false);
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

            to.ShoppingCart = await cart.MapAsync(
                isEditable: false,
                prepareEstimateShippingIfEnabled: false,
                prepareAndDisplayOrderReviewData: true);
        }
    }
}