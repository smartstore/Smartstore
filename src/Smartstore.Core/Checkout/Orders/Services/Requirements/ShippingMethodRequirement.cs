using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingMethodRequirement : CheckoutRequirementBase
    {
        private bool? _skip;
        private readonly SmartDbContext _db;
        private readonly IShippingService _shippingService;
        private readonly ShippingSettings _shippingSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public ShippingMethodRequirement(
            SmartDbContext db,
            IShippingService shippingService,
            IHttpContextAccessor httpContextAccessor,
            ShippingSettings shippingSettings,
            ShoppingCartSettings shoppingCartSettings)
            : base(httpContextAccessor)
        {
            _db = db;
            _shippingService = shippingService;
            _shippingSettings = shippingSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        protected override string ActionName => "ShippingMethod";

        public override int Order => 30;

        public override async Task<CheckoutRequirementResult> CheckAsync(ShoppingCart cart, object model = null)
        {
            var customer = cart.Customer;
            var attributes = customer.GenericAttributes;
            var options = attributes.OfferedShippingOptions;
            CheckoutWorkflowError[] errors = null;
            var saveAttributes = false;

            if (!cart.IsShippingRequired())
            {
                _skip = true;

                if (attributes.SelectedShippingOption != null || attributes.OfferedShippingOptions != null)
                {
                    attributes.SelectedShippingOption = null;
                    attributes.OfferedShippingOptions = null;
                    await attributes.SaveChangesAsync();
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

                var selectedShippingOption = options.FirstOrDefault(x => x.ShippingMethodId == selectedId);
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

            if (_skip == null)
            {
                _skip = _shippingSettings.SkipShippingIfSingleOption && options.Count == 1;
                if (_skip.Value)
                {
                    attributes.SelectedShippingOption = options[0];
                    saveAttributes = true;
                }
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled && attributes.SelectedShippingOption == null)
            {
                var preferredOption = attributes.PreferredShippingOption;
                if (preferredOption != null && preferredOption.ShippingMethodId != 0)
                {                   
                    if (preferredOption.ShippingRateComputationMethodSystemName.HasValue())
                    {
                        attributes.SelectedShippingOption = options.FirstOrDefault(x => x.ShippingMethodId == preferredOption.ShippingMethodId &&
                            x.ShippingRateComputationMethodSystemName.EqualsNoCase(preferredOption.ShippingRateComputationMethodSystemName));
                    }

                    attributes.SelectedShippingOption ??= options
                        .Where(x => x.ShippingMethodId == preferredOption.ShippingMethodId)
                        .OrderBy(x => x.Rate)
                        .FirstOrDefault();
                }

                // Fallback to last used shipping.
                attributes.SelectedShippingOption ??= await GetLastShippingOption(customer, options);
                saveAttributes = attributes.SelectedShippingOption != null;
            }

            if (saveAttributes)
            {
                await attributes.SaveChangesAsync();
            }

            return new(attributes.SelectedShippingOption != null, errors, _skip ?? false);
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

        private async Task<ShippingOption> GetLastShippingOption(Customer customer, List<ShippingOption> options)
        {
            // Perf: do not filter over field that has no index.
            var lastShippings = await _db.Orders
                .Where(x => x.CustomerId == customer.Id)
                .Select(x => new
                {
                    x.CreatedOnUtc,
                    x.ShippingMethod,
                    x.ShippingRateComputationMethodSystemName
                })
                .OrderByDescending(x => x.CreatedOnUtc)
                .Take(5)
                .ToListAsync();

            foreach (var lastShipping in lastShippings.Where(x => x.ShippingMethod.HasValue() && x.ShippingRateComputationMethodSystemName.HasValue()))
            {
                var option = options.FirstOrDefault(x => x.Name.EqualsNoCase(lastShipping.ShippingMethod) &&
                    x.ShippingRateComputationMethodSystemName.EqualsNoCase(lastShipping.ShippingRateComputationMethodSystemName));

                option ??= options.FirstOrDefault(x => x.Name.EqualsNoCase(lastShipping.ShippingMethod));

                if (option != null)
                {
                    return option;
                }
            }

            return null;
        }
    }
}
