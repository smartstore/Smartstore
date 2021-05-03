using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class CheckoutConfirmMappingExtensions
    {
        public static async Task MapAsync(this IEnumerable<OrganizedShoppingCartItem> entity, CheckoutConfirmModel model)
        {
            await MapperFactory.MapAsync(entity, model, null);
        }
    }

    public class CheckoutConfirmMapper : Mapper<IEnumerable<OrganizedShoppingCartItem>, CheckoutConfirmModel>
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

        protected override void Map(IEnumerable<OrganizedShoppingCartItem> from, CheckoutConfirmModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(IEnumerable<OrganizedShoppingCartItem> from, CheckoutConfirmModel to, dynamic parameters = null)
        {
            Guard.NotNull(to, nameof(to));
            Guard.NotNull(from, nameof(from));

            to.TermsOfServiceEnabled = _orderSettings.TermsOfServiceEnabled;
            to.ShowEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox;
            to.BypassPaymentMethodInfo = _paymentSettings.BypassPaymentMethodInfo;
            to.NewsLetterSubscription = _shoppingCartSettings.NewsLetterSubscription;
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