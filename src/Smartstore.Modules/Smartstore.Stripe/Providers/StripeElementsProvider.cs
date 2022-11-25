using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.StripeElements.Components;
using Smartstore.StripeElements.Controllers;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Settings;
using Stripe;

namespace Smartstore.StripeElements.Providers
{
    [SystemName("Smartstore.StripeElements")]
    [FriendlyName("Stripe Elements")]
    [Order(1)]
    public class StripeElementsProvider : PaymentMethodBase, IConfigurable
    {
        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ISettingFactory _settingFactory;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ICacheManager _cache;

        public StripeElementsProvider(
            SmartDbContext db,
            IStoreContext storeContext,
            ISettingFactory settingFactory,
            ICheckoutStateAccessor checkoutStateAccessor,
            ICacheManager cache)
        {
            _db = db;
            _storeContext = storeContext;
            _settingFactory = settingFactory;
            _checkoutStateAccessor = checkoutStateAccessor;
            _cache = cache;
        }

        public static string SystemName => "Smartstore.StripeElements";

        // TODO: (mh) (core) 
        public override bool SupportCapture => true;

        // TODO: (mh) (core) 
        public override bool SupportVoid => true;

        public override bool SupportPartiallyRefund => true;

        public override bool SupportRefund => true;

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.StandardAndButton;

        public RouteInfo GetConfigurationRoute()
            => new(nameof(StripeAdminController.Configure), "StripeAdmin", new { area = "Admin" });

        public override Widget GetPaymentInfoWidget()
            => new ComponentWidget(typeof(StripeElementsViewComponent));

        public override Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            var request = new ProcessPaymentRequest
            {
                OrderGuid = Guid.NewGuid()
            };

            return Task.FromResult(request);
        }

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            var settings = await _settingFactory.LoadSettingsAsync<StripeSettings>(_storeContext.CurrentStore.Id);

            return (settings.AdditionalFee, settings.AdditionalFeePercentage);
        }

        public override async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderGuid == Guid.Empty)
            {
                throw new Exception($"{nameof(processPaymentRequest.OrderGuid)} is missing.");
            }

            var result = new ProcessPaymentResult();
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();

            if (state.PaymentIntent.Id.IsEmpty())
            {
                throw new Exception(T("Payment.MissingCheckoutState", "StripeCheckoutState." + nameof(state.PaymentIntent.Id)));
            }

            // Update Payment Intent.
            var intentUpdateOptions = new PaymentIntentUpdateOptions
            {
                Amount = processPaymentRequest.OrderTotal.ToSmallestCurrencyUnit(),
                Currency = state.PaymentIntent.Currency,
                PaymentMethod = state.PaymentMethod,
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.UpdateAsync(state.PaymentIntent.Id, intentUpdateOptions);

            var confirmOptions = new PaymentIntentConfirmOptions
            {
                // TODO: (mh) (core) Correct redirect URL
                ReturnUrl = "https://localhost:44325/"
            };

            paymentIntent = await service.ConfirmAsync(paymentIntent.Id, confirmOptions);

            if (paymentIntent.NextAction.RedirectToUrl.Url.HasValue())
            {
                // TODO: (mh) (core) Implement redirect new ajax flow according to other implementation like Skrill, Postfinance etc.
            }

            // Store PaymentIntent.Id in AuthorizationTransactionId.
            result.AuthorizationTransactionId = state.PaymentIntent.Id;

            return result;
        }

        public override async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest request)
        {
            var result = new RefundPaymentResult
            {
                NewPaymentStatus = request.Order.PaymentStatus
            };

            var options = new RefundCreateOptions { PaymentIntent = request.Order.AuthorizationTransactionId };

            if (request.IsPartialRefund)
            {
                options.Amount = request.AmountToRefund.Amount.ToSmallestCurrencyUnit();
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            if (refund.Id.HasValue() && request.Order.Id != 0)
            {
                var refundIds = request.Order.GenericAttributes.Get<List<string>>("Payments.StripeElements.RefundId") ?? new List<string>();
                if (!refundIds.Contains(refund.Id))
                {
                    refundIds.Add(refund.Id);
                }

                request.Order.GenericAttributes.Set("Payments.StripeElements.RefundId", refundIds);
                await _db.SaveChangesAsync();

                result.NewPaymentStatus = request.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
            }

            return result;
        }
    }
}