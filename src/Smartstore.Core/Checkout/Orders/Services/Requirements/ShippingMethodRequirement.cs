using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingMethodRequirement : CheckoutRequirementBase
    {
        private bool? _active;
        private readonly IShippingService _shippingService;
        private readonly ShippingSettings _shippingSettings;

        public ShippingMethodRequirement(
            IShippingService shippingService,
            IHttpContextAccessor httpContextAccessor,
            ShippingSettings shippingSettings)
            : base(httpContextAccessor)
        {
            _shippingService = shippingService;
            _shippingSettings = shippingSettings;
        }

        protected override string ActionName => "ShippingMethod";

        public override int Order => 30;

        public override bool Active => _active ?? true;

        public override async Task<CheckoutRequirementResult> CheckAsync(ShoppingCart cart, object model = null)
        {
            var customer = cart.Customer;
            var attributes = customer.GenericAttributes;
            var options = attributes.OfferedShippingOptions;
            CheckoutWorkflowError[] errors = null;
            var saveAttributes = false;

            if (!cart.IsShippingRequired())
            {
                _active = false;

                if (attributes.SelectedShippingOption != null || attributes.OfferedShippingOptions != null)
                {
                    attributes.SelectedShippingOption = null;
                    attributes.OfferedShippingOptions = null;
                    await attributes.SaveChangesAsync();
                }

                return new(true);
            }

            if (model != null
                && model is string shippingOption 
                && IsSameRoute(HttpMethods.Post, ActionName))
            {
                var splittedOption = shippingOption.SplitSafe("___").ToArray();
                if (splittedOption.Length != 2)
                {
                    return new(false);
                }

                var selectedName = splittedOption[0];
                var providerSystemName = splittedOption[1];

                if (options.IsNullOrEmpty())
                {
                    // Shipping option was not found in customer attributes. Load via shipping service.
                    (options, errors) = await GetShippingOptions(cart, providerSystemName);
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

                return new(selectedShippingOption != null, errors);
            }

            if (options.IsNullOrEmpty())
            {
                (options, errors) = await GetShippingOptions(cart);
                if (options.Count == 0)
                {
                    return new(false, errors);
                }

                // Performance optimization. Cache returned shipping options.
                // We will use them later (after a customer has selected an option).
                attributes.OfferedShippingOptions = options;
                saveAttributes = true;
            }

            if (_active == null)
            {
                if (_shippingSettings.SkipShippingIfSingleOption && options.Count == 1)
                {
                    _active = false;
                    attributes.SelectedShippingOption = options[0];
                    saveAttributes = true;
                }
            }

            if (saveAttributes)
            {
                await attributes.SaveChangesAsync();
            }

            return new(attributes.SelectedShippingOption != null, errors);
        }

        private async Task<(List<ShippingOption> Options, CheckoutWorkflowError[] Errors)> GetShippingOptions(ShoppingCart cart, string providerSystemName = null)
        {
            CheckoutWorkflowError[] errors = null;
            var response = await _shippingService.GetShippingOptionsAsync(cart, cart.Customer.ShippingAddress, providerSystemName, cart.StoreId);

            if (response.ShippingOptions.Count == 0 && IsSameRoute(HttpMethods.Get, ActionName))
            {
                errors = response.Errors
                    .Select(x => new CheckoutWorkflowError(string.Empty, x))
                    .ToArray();
            }

            return (response.ShippingOptions, errors);
        }
    }
}
