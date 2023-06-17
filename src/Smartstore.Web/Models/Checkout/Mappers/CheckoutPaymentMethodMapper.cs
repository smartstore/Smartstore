using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Models.Checkout
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static Task MapAsync(this ShoppingCart cart, CheckoutPaymentMethodModel model)
            => MapperFactory.MapAsync(cart, model, null);
    }

    public class CheckoutPaymentMethodMapper : Mapper<ShoppingCart, CheckoutPaymentMethodModel>
    {
        private readonly ICommonServices _services;
        private readonly ModuleManager _moduleManager;
        private readonly ITaxService _taxService;
        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly ShippingSettings _shippingSettings;

        public CheckoutPaymentMethodMapper(
            ICommonServices services,
            ModuleManager moduleManager,
            IPaymentService paymentService,
            ITaxService taxService,
            IShippingService shippingService,
            IOrderCalculationService orderCalculationService,
            ITaxCalculator taxCalculator,
            ShippingSettings shippingSettings)
        {
            _services = services;
            _moduleManager = moduleManager;
            _paymentService = paymentService;
            _taxService = taxService;
            _shippingService = shippingService;
            _orderCalculationService = orderCalculationService;
            _taxCalculator = taxCalculator;
            _shippingSettings = shippingSettings;
        }

        protected override void Map(ShoppingCart from, CheckoutPaymentMethodModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ShoppingCart from, CheckoutPaymentMethodModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            // Was shipping skipped?
            var shippingOptions = (await _shippingService.GetShippingOptionsAsync(from, from.Customer.ShippingAddress, string.Empty, from.StoreId)).ShippingOptions;

            if (!from.IsShippingRequired() || (shippingOptions.Count <= 1 && _shippingSettings.SkipShippingIfSingleOption))
            {
                to.SkippedSelectShipping = true;
            }

            var paymentTypes = new PaymentMethodType[]
            {
                PaymentMethodType.Standard,
                PaymentMethodType.Redirection,
                PaymentMethodType.StandardAndRedirection,
                PaymentMethodType.StandardAndButton
            };

            var boundPaymentProviders = await _paymentService.LoadActivePaymentProvidersAsync(from, from.StoreId, paymentTypes);
            var allPaymentMethods = await _paymentService.GetAllPaymentMethodsAsync();

            foreach (var pp in boundPaymentProviders)
            {
                if (from.ContainsRecurringItem() && pp.Value.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel
                {
                    Name = _moduleManager.GetLocalizedFriendlyName(pp.Metadata),
                    Description = _moduleManager.GetLocalizedDescription(pp.Metadata),
                    PaymentMethodSystemName = pp.Metadata.SystemName,
                    InfoWidget = pp.Value.GetPaymentInfoWidget(),
                    RequiresInteraction = pp.Value.RequiresInteraction
                };

                if (allPaymentMethods.TryGetValue(pp.Metadata.SystemName, out var paymentMethod))
                {
                    pmModel.FullDescription = paymentMethod.GetLocalized(x => x.FullDescription, _services.WorkContext.WorkingLanguage);
                }

                pmModel.BrandUrl = _moduleManager.GetBrandImageUrl(pp.Metadata);

                // Payment method additional fee.
                var paymentTaxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.PaymentFee);
                var paymentMethodAdditionalFee = await _orderCalculationService.GetShoppingCartPaymentFeeAsync(from, pp.Metadata.SystemName);
                var rateBase = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentMethodAdditionalFee.Amount);
                var rate = _services.CurrencyService.ConvertFromPrimaryCurrency(rateBase.Price, _services.WorkContext.WorkingCurrency);

                if (rate != decimal.Zero)
                {
                    pmModel.Fee = rate.WithPostFormat(paymentTaxFormat);
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
