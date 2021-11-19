using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this ShoppingCart cart, CheckoutPaymentMethodModel model)
        {
            await MapperFactory.MapAsync(cart, model, null);
        }
    }

    public class CheckoutPaymentMethodMapper : Mapper<ShoppingCart, CheckoutPaymentMethodModel>
    {
        private readonly ICommonServices _services;
        private readonly ModuleManager _moduleManager;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly ShippingSettings _shippingSettings;

        public CheckoutPaymentMethodMapper(
            ICommonServices services,
            ModuleManager moduleManager,
            IPaymentService paymentService,
            ICurrencyService currencyService,
            IShippingService shippingService,
            IOrderCalculationService orderCalculationService,
            ITaxCalculator taxCalculator,
            ShippingSettings shippingSettings)
        {
            _services = services;
            _moduleManager = moduleManager;
            _paymentService = paymentService;
            _currencyService = currencyService;
            _shippingService = shippingService;
            _orderCalculationService = orderCalculationService;
            _taxCalculator = taxCalculator;
            _shippingSettings = shippingSettings;
        }

        protected override void Map(ShoppingCart from, CheckoutPaymentMethodModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ShoppingCart from, CheckoutPaymentMethodModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            // Was shipping skipped.
            var shippingOptions = (await _shippingService.GetShippingOptionsAsync(from, from.Customer.ShippingAddress, string.Empty, from.StoreId)).ShippingOptions;

            if (!from.IsShippingRequired() || (shippingOptions.Count <= 1 && _shippingSettings.SkipShippingIfSingleOption))
            {
                to.SkippedSelectShipping = true;
            }

            var paymentTypes = new PaymentMethodType[] { PaymentMethodType.Standard, PaymentMethodType.Redirection, PaymentMethodType.StandardAndRedirection };
            var boundPaymentMethods = await _paymentService.LoadActivePaymentMethodsAsync(from, from.StoreId, paymentTypes);
            var allPaymentMethods = await _paymentService.GetAllPaymentMethodsAsync(from.StoreId);

            foreach (var pm in boundPaymentMethods)
            {
                if (from.ContainsRecurringItem() && pm.Value.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel
                {
                    Name = _moduleManager.GetLocalizedFriendlyName(pm.Metadata),
                    Description = _moduleManager.GetLocalizedDescription(pm.Metadata),
                    PaymentMethodSystemName = pm.Metadata.SystemName,
                    InfoWidget = pm.Value.GetPaymentInfoWidget(),
                    RequiresInteraction = pm.Value.RequiresInteraction
                };

                if (allPaymentMethods.TryGetValue(pm.Metadata.SystemName, out var paymentMethod))
                {
                    pmModel.FullDescription = paymentMethod.GetLocalized(x => x.FullDescription, _services.WorkContext.WorkingLanguage);
                }

                pmModel.BrandUrl = _moduleManager.GetBrandImageUrl(pm.Metadata);

                // Payment method additional fee.
                var paymentMethodAdditionalFee = await _orderCalculationService.GetShoppingCartPaymentFeeAsync(from, pm.Metadata.SystemName);
                var paymentTaxFormat = _currencyService.GetTaxFormat(null, null, PricingTarget.PaymentFee);
                var rateBase = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentMethodAdditionalFee.Amount);

                if (paymentMethodAdditionalFee.Amount != decimal.Zero)
                {
                    pmModel.Fee = paymentMethodAdditionalFee.WithPostFormat(paymentTaxFormat);
                }

                to.PaymentMethods.Add(pmModel);
            }

            // Find a selected (previously) payment method.
            var selected = false;
            var selectedPaymentMethodSystemName = from.Customer.GenericAttributes.SelectedPaymentMethod;
            if (selectedPaymentMethodSystemName.HasValue())
            {
                var paymentMethodToSelect = to.PaymentMethods.Find(pm => pm.PaymentMethodSystemName.EqualsNoCase(selectedPaymentMethodSystemName));
                if (paymentMethodToSelect != null)
                {
                    paymentMethodToSelect.Selected = true;
                    selected = true;
                }
            }

            // If no option has been selected, let's select the first one.
            if (!selected)
            {
                var paymentMethodToSelect = to.PaymentMethods.FirstOrDefault();
                if (paymentMethodToSelect != null)
                {
                    paymentMethodToSelect.Selected = true;
                }
            }
        }
    }
}
