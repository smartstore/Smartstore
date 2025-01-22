using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Models.Checkout
{
    public class CheckoutPaymentMethodMapper : Mapper<CheckoutContext, CheckoutPaymentMethodModel>
    {
        private static PaymentMethodType[] PaymentTypes =>
        [
            PaymentMethodType.Standard,
            PaymentMethodType.Redirection,
            PaymentMethodType.StandardAndButton,
            PaymentMethodType.StandardAndRedirection
        ];

        private readonly IWorkContext _workContext;
        private readonly ModuleManager _moduleManager;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ICheckoutFactory _checkoutFactory;
        private readonly ITaxCalculator _taxCalculator;
        private readonly PaymentSettings _paymentSettings;
        
        public CheckoutPaymentMethodMapper(
            IWorkContext workContext,
            ModuleManager moduleManager,
            ICurrencyService currencyService,
            IPaymentService paymentService,
            ITaxService taxService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor,
            ICheckoutFactory checkoutFactory,
            ITaxCalculator taxCalculator,
            PaymentSettings paymentSettings)
        {
            _workContext = workContext;
            _moduleManager = moduleManager;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _taxService = taxService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _checkoutFactory = checkoutFactory;
            _taxCalculator = taxCalculator;
            _paymentSettings = paymentSettings;
        }

        protected override void Map(CheckoutContext from, CheckoutPaymentMethodModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(CheckoutContext from, CheckoutPaymentMethodModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var state = _checkoutStateAccessor.CheckoutState;
            var cart = from.Cart;
            var ga = cart.Customer.GenericAttributes;
            var allPaymentMethods = await _paymentService.GetAllPaymentMethodsAsync();
            var providers = await _paymentService.LoadActivePaymentProvidersAsync(cart, cart.StoreId, PaymentTypes);

            if (cart.ContainsRecurringItem())
            {
                providers = providers.Where(x => x.Value.RecurringPaymentType > RecurringPaymentType.NotSupported);
            }

            to.ActionName = CheckoutActionNames.PaymentMethod;
            to.PreviousStepUrl = _checkoutFactory.GetNextCheckoutStepUrl(from, false);
            to.DisplayPaymentMethodIcons = _paymentSettings.DisplayPaymentMethodIcons;

            foreach (var pp in providers)
            {
                var pmModel = new CheckoutPaymentMethodModel.PaymentMethodModel
                {
                    Name = _moduleManager.GetLocalizedFriendlyName(pp.Metadata),
                    Description = _moduleManager.GetLocalizedDescription(pp.Metadata),
                    PaymentMethodSystemName = pp.Metadata.SystemName,
                    InfoWidget = pp.Value.GetPaymentInfoWidget(),
                    RequiresInteraction = pp.Value.RequiresInteraction,
                    BrandUrl = _moduleManager.GetBrandImage(pp.Metadata)?.DefaultImageUrl
                };

                if (allPaymentMethods.TryGetValue(pp.Metadata.SystemName, out var paymentMethod))
                {
                    pmModel.FullDescription = paymentMethod.GetLocalized(x => x.FullDescription, _workContext.WorkingLanguage);
                }

                // Payment method additional fee.
                var paymentTaxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.PaymentFee);
                var paymentMethodAdditionalFee = await _orderCalculationService.GetShoppingCartPaymentFeeAsync(cart, pp.Metadata.SystemName);
                var rateBase = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentMethodAdditionalFee.Amount);
                var rate = _currencyService.ConvertFromPrimaryCurrency(rateBase.Price, _workContext.WorkingCurrency);

                if (rate != decimal.Zero)
                {
                    pmModel.Fee = rate.WithPostFormat(paymentTaxFormat);
                }

                to.PaymentMethods.Add(pmModel);
            }

            var toSelect = GetPaymentMethodModel(ga.SelectedPaymentMethod) 
                ?? GetPaymentMethodModel(ga.PreferredPaymentMethod)
                ?? to.PaymentMethods.FirstOrDefault();
            if (toSelect != null)
            {
                toSelect.Selected = true;
            }

            CheckoutPaymentMethodModel.PaymentMethodModel GetPaymentMethodModel(string systemName)
                => systemName.HasValue() ? to.PaymentMethods.FirstOrDefault(x => x.PaymentMethodSystemName.EqualsNoCase(systemName)) : null;
        }
    }
}
