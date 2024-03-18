using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Models.Checkout
{
    public class CheckoutShippingMethodMapper : Mapper<CheckoutContext, CheckoutShippingMethodModel>
    {
        private readonly IWorkContext _workContext;
        private readonly IProviderManager _providerManager;
        private readonly ModuleManager _moduleManager;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IShippingService _shippingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutFactory _checkoutFactory;

        public CheckoutShippingMethodMapper(
            IWorkContext workContext,
            IProviderManager providerManager,
            ModuleManager moduleManager,
            ICurrencyService currencyService,
            ITaxService taxService,
            ITaxCalculator taxCalculator,
            IShippingService shippingService,
            IOrderCalculationService orderCalculationService,
            ICheckoutFactory checkoutFactory)
        {
            _workContext = workContext;
            _providerManager = providerManager;
            _moduleManager = moduleManager;
            _currencyService = currencyService;
            _taxService = taxService;
            _taxCalculator = taxCalculator;
            _shippingService = shippingService;
            _orderCalculationService = orderCalculationService;
            _checkoutFactory = checkoutFactory;
        }

        protected override void Map(CheckoutContext from, CheckoutShippingMethodModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(CheckoutContext from, CheckoutShippingMethodModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var cart = from.Cart;
            var customer = cart.Customer;
            var options = customer.GenericAttributes.OfferedShippingOptions ??
                (await _shippingService.GetShippingOptionsAsync(cart, customer.ShippingAddress, storeId: cart.StoreId)).ShippingOptions;

            to.PreviousStepUrl = _checkoutFactory.GetNextCheckoutStepUrl(from, false);

            if (options.Count > 0)
            {
                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(cart.StoreId);

                foreach (var option in options)
                {
                    var model = new CheckoutShippingMethodModel.ShippingMethodModel
                    {
                        ShippingMethodId = option.ShippingMethodId,
                        Name = option.Name,
                        Description = option.Description,
                        ShippingRateComputationMethodSystemName = option.ShippingRateComputationMethodSystemName
                    };

                    var provider = _providerManager.GetProvider<IShippingRateComputationMethod>(option.ShippingRateComputationMethodSystemName);
                    if (provider != null)
                    {
                        model.BrandUrl = _moduleManager.GetBrandImage(provider.Metadata)?.DefaultImageUrl;
                    }

                    // Adjust rate.
                    var shippingTaxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.ShippingCharge);
                    var (shippingAmount, _) = await _orderCalculationService.AdjustShippingRateAsync(cart, option.Rate, option, shippingMethods);
                    var rateBase = await _taxCalculator.CalculateShippingTaxAsync(shippingAmount);
                    var rate = _currencyService.ConvertFromPrimaryCurrency(rateBase.Price, _workContext.WorkingCurrency);
                    model.Fee = rate.WithPostFormat(shippingTaxFormat);

                    to.ShippingMethods.Add(model);
                }

                // Find a selected (previously) shipping method.
                var selectedShippingOption = customer.GenericAttributes.SelectedShippingOption;
                if (selectedShippingOption != null)
                {
                    var shippingOptionToSelect = to.ShippingMethods
                        .ToList()
                        .Find(so => so.Name.HasValue() &&
                            so.Name.EqualsNoCase(selectedShippingOption.Name) &&
                            so.ShippingRateComputationMethodSystemName.HasValue() &&
                            so.ShippingRateComputationMethodSystemName.EqualsNoCase(selectedShippingOption.ShippingRateComputationMethodSystemName));

                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }

                // If no option has been selected, let's do it for the first one.
                if (to.ShippingMethods.FirstOrDefault(so => so.Selected) == null)
                {
                    var shippingOptionToSelect = to.ShippingMethods.FirstOrDefault();
                    if (shippingOptionToSelect != null)
                    {
                        shippingOptionToSelect.Selected = true;
                    }
                }
            }
        }
    }
}
