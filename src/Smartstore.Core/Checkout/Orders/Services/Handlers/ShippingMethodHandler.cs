using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    public class ShippingMethodHandler : CheckoutHandlerBase
    {
        private readonly IShippingService _shippingService;
        private readonly ShippingSettings _shippingSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public ShippingMethodHandler(
            IShippingService shippingService,
            IHttpContextAccessor httpContextAccessor,
            ShippingSettings shippingSettings,
            ShoppingCartSettings shoppingCartSettings)
            : base(httpContextAccessor)
        {
            _shippingService = shippingService;
            _shippingSettings = shippingSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        protected override string ActionName => "ShippingMethod";

        public override int Order => 30;

        public override async Task<CheckoutHandlerResult> ProcessAsync(ShoppingCart cart, object model = null)
        {
            var customer = cart.Customer;
            var ga = customer.GenericAttributes;
            var options = ga.OfferedShippingOptions;
            CheckoutWorkflowError[] errors = null;
            var saveAttributes = false;

            if (!cart.IsShippingRequired())
            {
                if (ga.SelectedShippingOption != null || ga.OfferedShippingOptions != null)
                {
                    ga.SelectedShippingOption = null;
                    ga.OfferedShippingOptions = null;
                    await ga.SaveChangesAsync();
                }

                return new(true, null, true);
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

                var selectedId = splittedOption[0].ToInt();
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

                var selectedOption = options.FirstOrDefault(x => x.ShippingMethodId == selectedId);
                if (selectedOption != null)
                {
                    ga.SelectedShippingOption = selectedOption;
                    ga.PreferredShippingOption ??= selectedOption;

                    await ga.SaveChangesAsync();
                }

                return new(selectedOption != null, errors);
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
                ga.OfferedShippingOptions = options;
                saveAttributes = true;
            }

            var skip = _shippingSettings.SkipShippingIfSingleOption && options.Count == 1;
            if (skip)
            {
                ga.SelectedShippingOption = options[0];
                saveAttributes = true;
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled && ga.SelectedShippingOption == null)
            {
                var preferredOption = ga.PreferredShippingOption;
                if (preferredOption != null && preferredOption.ShippingMethodId != 0)
                {                   
                    if (preferredOption.ShippingRateComputationMethodSystemName.HasValue())
                    {
                        ga.SelectedShippingOption = options.FirstOrDefault(x => x.ShippingMethodId == preferredOption.ShippingMethodId &&
                            x.ShippingRateComputationMethodSystemName.EqualsNoCase(preferredOption.ShippingRateComputationMethodSystemName));
                    }

                    ga.SelectedShippingOption ??= options
                        .Where(x => x.ShippingMethodId == preferredOption.ShippingMethodId)
                        .OrderBy(x => x.Rate)
                        .FirstOrDefault();
                }

                saveAttributes = ga.SelectedShippingOption != null;
            }

            if (saveAttributes)
            {
                await ga.SaveChangesAsync();
            }

            return new(ga.SelectedShippingOption != null, errors, skip);
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
