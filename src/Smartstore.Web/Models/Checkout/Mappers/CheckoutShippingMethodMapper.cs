using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Cart = Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this Cart.ShoppingCart cart, CheckoutShippingMethodModel model)
        {
            await MapperFactory.MapAsync(cart, model, null);
        }
    }

    public class CheckoutShippingMethodMapper : Mapper<Cart.ShoppingCart, CheckoutShippingMethodModel>
    {
        private readonly ICommonServices _services;
        private readonly ICurrencyService _currencyService;
        private readonly IShippingService _shippingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ITaxCalculator _taxCalculator;

        public CheckoutShippingMethodMapper(
            ICommonServices services,
            ICurrencyService currencyService,
            IShippingService shippingService,
            IOrderCalculationService orderCalculationService,
            ITaxCalculator taxCalculator)
        {
            _services = services;
            _currencyService = currencyService;
            _shippingService = shippingService;
            _orderCalculationService = orderCalculationService;
            _taxCalculator = taxCalculator;
        }

        protected override void Map(Cart.ShoppingCart from, CheckoutShippingMethodModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override Task MapAsync(Cart.ShoppingCart from, CheckoutShippingMethodModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var storeId = _services.StoreContext.CurrentStore.Id;
            var customer = _services.WorkContext.CurrentCustomer;

            // TODO: (mh) (core) Wait with implementation until any provider for shipping rate computation has been implemented.
            //var getShippingOptionResponse = _shippingService.GetShippingOptions(from.ToList(), customer.ShippingAddress, storeId: storeId);
            //if (!getShippingOptionResponse.Success)
            //{
            //    foreach (var error in getShippingOptionResponse.Errors)
            //    {
            //        to.Warnings.Add(error);
            //    }

            //    return;
            //}

            //// Performance optimization. Cache returned shipping options.
            //// We will use them later (after the customer has selected an option).
            //customer.GenericAttributes.OfferedShippingOptions = getShippingOptionResponse.ShippingOptions;
            //await customer.GenericAttributes.SaveChangesAsync();

            //var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(storeId);

            //foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
            //{
            //    var model = new CheckoutShippingMethodModel.ShippingMethodModel
            //    {
            //        ShippingMethodId = shippingOption.ShippingMethodId,
            //        Name = shippingOption.Name,
            //        Description = shippingOption.Description,
            //        ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName,
            //    };

            //    // TODO: (mh) (core) Wait with implmenentation until any provider for shipping rate computation has been implemented.
            //    //var provider = _shippingService.LoadActiveShippingRateComputationMethods(systemName: shippingOption.ShippingRateComputationMethodSystemName);
            //    //if (provider != null)
            //    //{
            //    //    // TODO: (mh) (core) Wait for PluginMediator implementation
            //    //    //model.BrandUrl = _pluginMediator.GetBrandImageUrl(srcmProvider.Metadata);
            //    //}

            //    // Adjust tax rate.
            //    var (Shipping, Discount) = await _orderCalculationService.AdjustShippingRateAsync(from.ToList(), shippingOption.Rate, shippingOption, shippingMethods);
            //    var tax = await _taxCalculator.CalculateShippingTaxAsync(Shipping.Amount);
            //    var rate = _currencyService.ConvertFromPrimaryCurrency(tax.Rate.Rate, _services.WorkContext.WorkingCurrency);
            //    model.FeeRaw = rate.Amount;
            //    model.Fee = rate.ToString(true);

            //    to.ShippingMethods.Add(model);
            //}

            // Find a (previously) selected shipping method.
            var selectedShippingOption = customer.GenericAttributes.SelectedShippingOption;
            if (customer.GenericAttributes.SelectedShippingOption != null)
            {
                var shippingOptionToSelect = to.ShippingMethods.Find(x => x.Name.EqualsNoCase(selectedShippingOption.Name)
                    && x.ShippingRateComputationMethodSystemName.EqualsNoCase(selectedShippingOption.ShippingRateComputationMethodSystemName));

                if (shippingOptionToSelect != null)
                {
                    shippingOptionToSelect.Selected = true;
                }
            }

            // If no option has been selected, just try selecting the first one.
            if (to.ShippingMethods.FirstOrDefault(x => x.Selected) == null)
            {
                var shippingOptionToSelect = to.ShippingMethods.FirstOrDefault();
                if (shippingOptionToSelect != null)
                {
                    shippingOptionToSelect.Selected = true;
                }
            }

            return Task.CompletedTask;
        }
    }
}
