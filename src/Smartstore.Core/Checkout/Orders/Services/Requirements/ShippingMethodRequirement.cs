using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingMethodRequirement : CheckoutRequirementBase
    {
        const string ActionName = "ShippingMethod";

        private readonly IShippingService _shippingService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ShippingSettings _shippingSettings;

        public ShippingMethodRequirement(
            IShippingService shippingService,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor,
            ShippingSettings shippingSettings)
            : base(httpContextAccessor)
        {
            _shippingService = shippingService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shippingSettings = shippingSettings;
        }

        public override int Order => 30;

        protected override RedirectToActionResult FulfillResult
            => CheckoutWorkflow.RedirectToCheckout(ActionName);

        public override async Task<bool> IsFulfilledAsync(ShoppingCart cart, IList<CheckoutWorkflowError> errors, object model = null)
        {
            var customer = cart.Customer;
            var attributes = customer.GenericAttributes;
            var options = attributes.OfferedShippingOptions;
            var saveAttributes = false;

            if (model != null
                && model is string shippingOption 
                && IsSameRoute(HttpMethods.Post, ActionName))
            {
                var splittedOption = shippingOption.SplitSafe("___").ToArray();
                if (splittedOption.Length != 2)
                {
                    return false;
                }

                var selectedName = splittedOption[0];
                var providerSystemName = splittedOption[1];

                if (options.IsNullOrEmpty())
                {
                    // Shipping option was not found in customer attributes. Load via shipping service.
                    options = await GetShippingOptions(cart, errors, providerSystemName);
                }
                else
                {
                    // Loaded cached results. Filter result by a chosen shipping rate computation method.
                    options = options.Where(x => x.ShippingRateComputationMethodSystemName.EqualsNoCase(providerSystemName)).ToList();
                }

                var selectedShippingOption = options.Find(x => x.Name.EqualsNoCase(selectedName));
                if (selectedShippingOption != null)
                {
                    // Save selected shipping option in customer attributes.
                    attributes.SelectedShippingOption = selectedShippingOption;
                    await attributes.SaveChangesAsync();
                }

                return selectedShippingOption != null;
            }

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
            
            if (options.IsNullOrEmpty())
            {
                options = await GetShippingOptions(cart, errors);
                if (options.Count == 0)
                {
                    return false;
                }

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

        private async Task<List<ShippingOption>> GetShippingOptions(ShoppingCart cart, IList<CheckoutWorkflowError> errors, string providerSystemName = null)
        {
            var response = await _shippingService.GetShippingOptionsAsync(cart, cart.Customer.ShippingAddress, providerSystemName, cart.StoreId);

            if (response.ShippingOptions.Count == 0 && IsSameRoute(HttpMethods.Get, ActionName))
            {
                response.Errors.Each(x => errors.Add(new(string.Empty, x)));
            }

            return response.ShippingOptions;
        }
    }
}
