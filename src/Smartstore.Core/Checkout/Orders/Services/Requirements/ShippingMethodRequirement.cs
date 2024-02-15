using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingMethodRequirement : CheckoutRequirementBase
    {
        private readonly IShippingService _shippingService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ShippingSettings _shippingSettings;

        public ShippingMethodRequirement(
            IShippingService shippingService,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            ShippingSettings shippingSettings)
            : base(CheckoutRequirement.ShippingMethod, httpContextAccessor)
        {
            _shippingService = shippingService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shippingSettings = shippingSettings;
        }

        public override async Task<bool> IsFulfilledAsync(ShoppingCart cart)
        {
            var customer = cart.Customer;
            var attributes = customer.GenericAttributes;

            if (!cart.IsShippingRequired())
            {
                if (attributes.SelectedShippingOption != null || attributes.OfferedShippingOptions != null)
                {
                    attributes.SelectedShippingOption = null;
                    attributes.OfferedShippingOptions = null;
                    await attributes.SaveChangesAsync();
                }

                return true;
            }

            if (attributes.SelectedShippingOption != null)
            {
                return true;
            }

            var saveAttributes = false;
            var options = attributes.OfferedShippingOptions;

            if (options == null)
            {
                options = (await _shippingService.GetShippingOptionsAsync(cart, customer.ShippingAddress, storeId: cart.StoreId)).ShippingOptions;

                // TODO: (mg)(quick-checkout) CheckoutShippingMethodMapper: updating customer.GenericAttributes.OfferedShippingOptions is redundant. Done by this requirement.
                // TODO: (mg)(quick-checkout) CheckoutShippingMethodMapper: use customer.GenericAttributes.OfferedShippingOptions instead of "ShippingOptionResponse" parameter.

                // Performance optimization. Cache returned shipping options.
                // We will use them later (after a customer has selected an option).
                attributes.OfferedShippingOptions = options;
                saveAttributes = true;
            }

            if (_shippingSettings.SkipShippingIfSingleOption && options.Count == 1)
            {
                attributes.SelectedShippingOption = options[0];
                saveAttributes = true;
            }

            if (saveAttributes)
            {
                await attributes.SaveChangesAsync();
            }

            // TODO: (mg)(quick-checkout) "HasOnlyOneActiveShippingMethod" is redundant. Instead use customer.GenericAttributes.OfferedShippingOptions.Count == 1
            _checkoutStateAccessor.CheckoutState.CustomProperties["HasOnlyOneActiveShippingMethod"] = options.Count == 1;

            return attributes.SelectedShippingOption != null;
        }

        public override Task<IActionResult> AdvanceAsync(ShoppingCart cart, object model)
        {
            throw new NotImplementedException();
        }
    }
}
