using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Paystack.Components;
using Smartstore.Paystack.Client;
using Smartstore.Paystack.Configuration;
using Newtonsoft.Json;
using Smartstore.Paystack.Models;
using System.Net.Http;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Paystack.Providers
{
    [SystemName("Payments.Paystack")]
    [FriendlyName("Paystack Pay")]
    [Order(-1)]
    public class PaystackProvider : PaymentMethodBase, IConfigurable
    {

        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IPaystackClient _paystackClient;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly PaystackSettings _paystackSettings;

        public PaystackProvider(
            SmartDbContext db,
            ICommonServices services,
            IHttpContextAccessor httpContextAccessor,
            ICheckoutStateAccessor checkoutStateAccessor,
            IPaystackClient paystackClient,
            IOrderCalculationService orderCalculationService,
            PaystackSettings paystackSettings)
        {
            _db = db;
            _services = services;
            _httpContextAccessor = httpContextAccessor;
            _checkoutStateAccessor = checkoutStateAccessor;
            _paystackClient = paystackClient;
            _orderCalculationService = orderCalculationService;
            _paystackSettings = paystackSettings;
        }

        public static string SystemName => "Payments.Paystack";
        public override bool SupportRefund => true;
        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;
        public RouteInfo GetConfigurationRoute() => new("Configure", "PaystackAdmin", new { area = "Admin" });

        public override Widget GetPaymentInfoWidget() => new ComponentWidget(typeof(PaystackViewComponent));

        public override Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;


            if (_paystackSettings.BaseUrl.IsEmpty())
            {
                result.Errors.Add(T("Plugins.Payments.PaystackRedirect.InvalidBaseUrl"));
            }
            if (_paystackSettings.PublicKey.IsEmpty())
            {
                result.Errors.Add(T("Plugins.Payments.PaystackRedirect.InvalidPublicKey"));
            }
            if (_paystackSettings.PrivateKey.IsEmpty())
            {
                result.Errors.Add(T("Plugins.Payments.PaystackRedirect.InvalidPrivateKey"));
            }
            return Task.FromResult(result);
        }

        public override async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (postProcessPaymentRequest.Order.PaymentStatus == PaymentStatus.Paid)
                await Task.CompletedTask;

            // Specify redirection URL here if your provider is of type PaymentMethodType.Redirection.
            // Core redirects to this URL automatically.
            //postProcessPaymentRequest.RedirectUrl = "";

            var redirectUrl = await InitializePaymentAsync(postProcessPaymentRequest.Order.OrderTotal, postProcessPaymentRequest.Order.Customer.Email, postProcessPaymentRequest.Order.OrderGuid);
            postProcessPaymentRequest.RedirectUrl = redirectUrl;

            await Task.CompletedTask;
        }

        async Task<string> InitializePaymentAsync(decimal amount, string email, Guid referenceNumber)
        {
            var store = _services.StoreContext.CurrentStore;

            var returnUrl = _services.WebHelper.GetStoreLocation(store.SslEnabled) + "Paystack/PaymentConfirmation";

            var requestModel = new PaystackInitializeRequestModel
            {
                Amount = (amount * 100).ToString("#####"),
                Email = email,
                Reference = referenceNumber.ToString("n"),
                CallBackUrl = returnUrl
            };

            var apiResponse = await _paystackClient.InitializeAsync(requestModel);

            await apiResponse.EnsureSuccessStatusCodeAsync();

            return apiResponse.Content.Data.AuthorizationUrl;

        }

        public override async Task<(decimal FixedFeeOrPercentage, bool UsePercentage)> GetPaymentFeeInfoAsync(ShoppingCart cart)
        {
            return (_paystackSettings.Fee, _paystackSettings.AdditionalFeePercentage);
            // return await Task.FromResult((100m, false));
            // Percentage
            var shoppingCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);

            var fee = _paystackSettings.Fee;
            var additionalFee = _paystackSettings.AdditionalFee;

            const decimal feeCap = 2000m;
            // const decimal flatFee = 0m;//formerly 100
            decimal finalFee = 0m;

            decimal decimalFee = fee / 100;

            var applicableFee = (decimalFee * shoppingCartTotal.Total.Value.Amount) + additionalFee;
            finalFee = applicableFee < feeCap ? applicableFee : feeCap;


            return (decimal.Round(finalFee, 2), false);

        }

    }
}
