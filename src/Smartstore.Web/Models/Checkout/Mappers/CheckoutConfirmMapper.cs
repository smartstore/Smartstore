using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Models.Cart;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Models.Checkout
{
    public class CheckoutConfirmMapper : Mapper<CheckoutContext, CheckoutConfirmModel>
    {
        const string TermsLinkTemplate = "<a class='terms-trigger read' href='{0}' data-toggle='modal' data-target='#terms-of-service-modal'>";

        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly ICheckoutFactory _checkoutFactory;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPaymentService _paymentService;
        private readonly IUrlHelper _urlHelper;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ModuleManager _moduleManager;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CheckoutConfirmMapper(
            IStoreContext storeContext,
            IWorkContext workContext,
            ICheckoutFactory checkoutFactory,
            IShoppingCartService shoppingCartService,
            IPaymentService paymentService,
            IUrlHelper urlHelper,
            ICheckoutStateAccessor checkoutStateAccessor,
            ModuleManager moduleManager,
            ShoppingCartSettings shoppingCartSettings)
        {
            _storeContext = storeContext;
            _workContext = workContext;
            _checkoutFactory = checkoutFactory;
            _shoppingCartService = shoppingCartService;
            _paymentService = paymentService;
            _urlHelper = urlHelper;
            _checkoutStateAccessor = checkoutStateAccessor;
            _moduleManager = moduleManager;
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
            var customer = _workContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            var isBillingAddresRequired = cart.Requirements.HasFlag(CheckoutRequirements.BillingAddress);
            var isPaymentRequired = cart.Requirements.HasFlag(CheckoutRequirements.Payment);

            to.ActionName = CheckoutActionNames.Confirm;
            to.PreviousStepUrl = _checkoutFactory.GetNextCheckoutStepUrl(from, false);
            to.ShowSecondBuyButtonBelowCart = _shoppingCartSettings.ShowSecondBuyButtonBelowCart;
            to.ShowEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox;
            to.NewsletterSubscription = _shoppingCartSettings.NewsletterSubscription;
            to.ThirdPartyEmailHandOver = _shoppingCartSettings.ThirdPartyEmailHandOver;

            if (!_shoppingCartSettings.IsTerminalCheckoutActivated())
            {
                to.TermsOfService = T("Checkout.TermsOfService.IAccept",
                    TermsLinkTemplate.FormatInvariant(await _urlHelper.TopicAsync("ConditionsOfUse", true)),
                    "</a>",
                    TermsLinkTemplate.FormatInvariant(await _urlHelper.TopicAsync("Disclaimer", true)),
                    TermsLinkTemplate.FormatInvariant(await _urlHelper.TopicAsync("PrivacyInfo", true)));
            }

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

            to.ShoppingCart = await cart.MapAsync(isEditable: false, prepareEstimateShippingIfEnabled: false);

            if (cart.IsShippingRequired || isBillingAddresRequired || isPaymentRequired)
            {
                var state = _checkoutStateAccessor.CheckoutState;
                var pm = await _paymentService.LoadPaymentProviderBySystemNameAsync(customer.GenericAttributes.SelectedPaymentMethod);
                var paymentMethod = pm != null ? _moduleManager.GetLocalizedFriendlyName(pm.Metadata).NullEmpty() : null;

                state.CustomProperties.TryGetValueAs("HasOnlyOneActivePaymentMethod", out bool singlePaymentMethod);

                to.OrderReviewData = new()
                {
                    IsBillingAddressRequired = isBillingAddresRequired,
                    IsShippable = cart.IsShippingRequired,
                    PaymentSummary = state.PaymentSummary,
                    PaymentMethod = paymentMethod ?? customer.GenericAttributes.SelectedPaymentMethod,
                    IsPaymentSelectionSkipped = state.IsPaymentSelectionSkipped,
                    IsPaymentRequired = state.IsPaymentRequired && isPaymentRequired,
                    DisplayPaymentMethodChangeOption = !singlePaymentMethod
                };

                if (customer.BillingAddress != null && isBillingAddresRequired)
                {
                    to.OrderReviewData.BillingAddress = await MapperFactory.MapAsync<Address, AddressModel>(customer.BillingAddress);
                }

                if (to.OrderReviewData.IsShippable)
                {
                    to.OrderReviewData.ShippingMethod = customer.GenericAttributes.SelectedShippingOption?.Name;
                    to.OrderReviewData.DisplayShippingMethodChangeOption = (customer.GenericAttributes.OfferedShippingOptions?.Count ?? int.MaxValue) > 1;

                    if (customer.ShippingAddress != null)
                    {
                        to.OrderReviewData.ShippingAddress = await MapperFactory.MapAsync<Address, AddressModel>(customer.ShippingAddress);
                    }
                }
            }
        }
    }
}