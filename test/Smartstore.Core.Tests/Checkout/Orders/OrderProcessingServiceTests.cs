using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Events;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Checkout.Orders
{
    [TestFixture]
    public class OrderProcessingServiceTests : ServiceTestBase
    {
        IWorkContext _workContext;
        ITaxService _taxService;
        ITaxCalculator _taxCalculator;
        IRoundingHelper _roundingHelper;
        IPaymentService _paymentService;
        IOrderProcessingService _orderProcessingService;

        CurrencySettings _currencySettings;
        TaxSettings _taxSettings;
        RewardPointsSettings _rewardPointsSettings;
        OrderSettings _orderSettings;
        LocalizationSettings _localizationSettings;
        ShoppingCartSettings _shoppingCartSettings;
        CatalogSettings _catalogSettings;
        PaymentSettings _paymentSettings;

        Mock<IPaymentService> _paymentServiceMock;

        Currency _currency;
        Language _language;

        [SetUp]
        public new void SetUp()
        {
            var localizationServiceMock = new Mock<ILocalizationService>();
            var webHelperMock = new Mock<IWebHelper>();
            var currencyServiceMock = new Mock<ICurrencyService>();
            var productServiceMock = new Mock<IProductService>();
            var productAttributeMaterializerMock = new Mock<IProductAttributeMaterializer>();
            var productAttributeFormatterMock = new Mock<IProductAttributeFormatter>();
            var priceCalcServiceMock = new Mock<IPriceCalculationService>();
            var orderCalcServiceMock = new Mock<IOrderCalculationService>();
            var shippingServiceMock = new Mock<IShippingService>();
            var giftCardServiceMock = new Mock<IGiftCardService>();
            var encryptorMock = new Mock<IEncryptor>();
            var activityLoggerMock = new Mock<IActivityLogger>();
            var shoppingCartServiceMock = new Mock<IShoppingCartService>();
            var shoppingCartValidatorMock = new Mock<IShoppingCartValidator>();
            var messageFactoryMock = new Mock<IMessageFactory>();
            var newsletterSubscriptionServiceMock = new Mock<INewsletterSubscriptionService>();
            var checkoutAttributeFormatterMock = new Mock<ICheckoutAttributeFormatter>();

            _currency = new Currency { Id = 1, RoundNumDecimals = 3 };
            _language = new Language { Id = 1 };

            var workContextMock = new Mock<IWorkContext>();
            _workContext = workContextMock.Object;
            workContextMock.Setup(x => x.WorkingCurrency).Returns(_currency);
            workContextMock.Setup(x => x.WorkingLanguage).Returns(_language);

            // Settings
            _taxSettings = new TaxSettings
            {
                ShippingIsTaxable = true,
                PaymentMethodAdditionalFeeIsTaxable = true,
                DefaultTaxAddressId = 10
            };

            _currencySettings = new CurrencySettings();
            _rewardPointsSettings = new RewardPointsSettings();
            _orderSettings = new OrderSettings();
            _localizationSettings = new LocalizationSettings();
            _shoppingCartSettings = new ShoppingCartSettings();
            _catalogSettings = new CatalogSettings();
            _paymentSettings = new PaymentSettings();

            var taxServiceMock = new Mock<ITaxService>();
            _taxService = taxServiceMock.Object;

            _paymentServiceMock = new Mock<IPaymentService>();
            _paymentService = _paymentServiceMock.Object;

            _roundingHelper = new RoundingHelper(_workContext, _currencySettings);

            // INFO: no mocking here to use real implementation.
            _taxCalculator = new TaxCalculator(DbContext, _workContext, _roundingHelper, _taxService, _taxSettings);

            _orderProcessingService = new OrderProcessingService(
                DbContext,
                _workContext,
                webHelperMock.Object,
                localizationServiceMock.Object,
                currencyServiceMock.Object,
                _roundingHelper,
                _paymentService,
                productServiceMock.Object,
                productAttributeMaterializerMock.Object,
                productAttributeFormatterMock.Object,
                priceCalcServiceMock.Object,
                orderCalcServiceMock.Object,
                _taxCalculator,
                shoppingCartServiceMock.Object,
                shoppingCartValidatorMock.Object,
                shippingServiceMock.Object,
                giftCardServiceMock.Object,
                newsletterSubscriptionServiceMock.Object,
                checkoutAttributeFormatterMock.Object,
                encryptorMock.Object,
                messageFactoryMock.Object,
                NullEventPublisher.Instance,
                activityLoggerMock.Object,
                _rewardPointsSettings,
                _catalogSettings,
                _orderSettings,
                _shoppingCartSettings,
                _localizationSettings,
                _taxSettings,
                _paymentSettings);
        }

        [Test]
        public void Ensure_order_can_only_be_cancelled_when_orderStatus_is_not_cancelled_yet()
        {
            var order = new Order();
            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;
                        if (os != OrderStatus.Cancelled)
                        {
                            order.CanCancelOrder().ShouldBeTrue();
                        }
                        else
                        {
                            order.CanCancelOrder().ShouldBeFalse();
                        }
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_can_only_be_marked_as_authorized_when_orderStatus_is_not_cancelled_and_paymentStatus_is_pending()
        {
            var order = new Order();
            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;
                        if (os != OrderStatus.Cancelled && ps == PaymentStatus.Pending)
                        {
                            order.CanMarkOrderAsAuthorized().ShouldBeTrue();
                        }
                        else
                        {
                            order.CanMarkOrderAsAuthorized().ShouldBeFalse();
                        }
                    }
                }
            }
        }

        [Test]
        public async Task Ensure_order_can_only_be_captured_when_orderStatus_is_not_cancelled_or_pending_and_paymentstatus_is_authorized_and_paymentModule_supports_capture()
        {
            var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_capture", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod1);

            var testMethod2 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod2");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_doesn't_support_capture", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod2);

            var order = new Order
            {
                PaymentMethodSystemName = "paymentMethodSystemName_that_supports_capture"
            };

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        if (os != OrderStatus.Cancelled && os != OrderStatus.Pending && (ps == PaymentStatus.Authorized))
                        {
                            (await _orderProcessingService.CanCaptureAsync(order)).ShouldBeTrue();
                        }
                        else
                        {
                            (await _orderProcessingService.CanCaptureAsync(order)).ShouldBeFalse();
                        }
                    }
                }
            }

            order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_capture";
            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        (await _orderProcessingService.CanCaptureAsync(order)).ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_cannot_be_marked_as_paid_when_orderStatus_is_cancelled_or_paymentStatus_is_paid_or_refunded_or_voided()
        {
            var order = new Order();
            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;
                        if (os == OrderStatus.Cancelled || ps == PaymentStatus.Paid || ps == PaymentStatus.Refunded || ps == PaymentStatus.Voided)
                        {
                            order.CanMarkOrderAsPaid().ShouldBeFalse();
                        }
                        else
                        {
                            order.CanMarkOrderAsPaid().ShouldBeTrue();
                        }
                    }
                }
            }
        }

        [Test]
        public async Task Ensure_order_can_only_be_refunded_when_paymentstatus_is_paid_and_paymentModule_supports_refund()
        {
            var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_refund", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod1);

            var testMethod2 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod2");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_doesn't_support_refund", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod2);

            var order = new Order();
            order.OrderTotal = 1;
            order.PaymentMethodSystemName = "paymentMethodSystemName_that_supports_refund";

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        if (ps == PaymentStatus.Paid)
                        {
                            (await _orderProcessingService.CanRefundAsync(order)).ShouldBeTrue();
                        }
                        else
                        {
                            (await _orderProcessingService.CanRefundAsync(order)).ShouldBeFalse();
                        }
                    }
                }
            }

            order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_refund";
            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        (await _orderProcessingService.CanRefundAsync(order)).ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public async Task Ensure_order_cannot_be_refunded_when_orderTotal_is_zero()
        {
            var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_refund", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod1);

            var order = new Order();
            order.PaymentMethodSystemName = "paymentMethodSystemName_that_supports_refund";

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        (await _orderProcessingService.CanRefundAsync(order)).ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_can_only_be_refunded_offline_when_paymentstatus_is_paid()
        {
            var order = new Order()
            {
                OrderTotal = 1,
            };

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        if (ps == PaymentStatus.Paid)
                            order.CanRefundOffline().ShouldBeTrue();
                        else
                            order.CanRefundOffline().ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_cannot_be_refunded_offline_when_orderTotal_is_zero()
        {
            var order = new Order();

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        order.CanRefundOffline().ShouldBeFalse();
                    }
                }
            }
        }


        [Test]
        public async Task Ensure_order_can_only_be_voided_when_paymentstatus_is_authorized_and_paymentModule_supports_void()
        {
            var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_void", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod1);

            var testMethod2 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod2");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_doesn't_support_void", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod2);

            var order = new Order();
            order.OrderTotal = 1;
            order.PaymentMethodSystemName = "paymentMethodSystemName_that_supports_void";

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        if (ps == PaymentStatus.Authorized)
                        {
                            (await _orderProcessingService.CanVoidAsync(order)).ShouldBeTrue();
                        }
                        else
                        {
                            (await _orderProcessingService.CanVoidAsync(order)).ShouldBeFalse();
                        }
                    }
                }
            }

            order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_void";
            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        (await _orderProcessingService.CanVoidAsync(order)).ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public async Task Ensure_order_cannot_be_voided_when_orderTotal_is_zero()
        {
            var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_void", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod1);

            var order = new Order();
            order.PaymentMethodSystemName = "paymentMethodSystemName_that_supports_void";

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        (await _orderProcessingService.CanVoidAsync(order)).ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_can_only_be_voided_offline_when_paymentstatus_is_authorized()
        {
            var order = new Order()
            {
                OrderTotal = 1,
            };

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        if (ps == PaymentStatus.Authorized)
                        {
                            order.CanVoidOffline().ShouldBeTrue();
                        }
                        else
                        {
                            order.CanVoidOffline().ShouldBeFalse();
                        }
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_cannot_be_voided_offline_when_orderTotal_is_zero()
        {
            var order = new Order();

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        order.CanVoidOffline().ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public async Task Ensure_order_can_only_be_partially_refunded_when_paymentstatus_is_paid_or_partiallyRefunded_and_paymentModule_supports_partialRefund()
        {
            var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_partialrefund", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod1);

            var testMethod2 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod2");
            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_doesn't_support_partialrefund", It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(testMethod2);

            var order = new Order();
            order.OrderTotal = 100;
            order.PaymentMethodSystemName = "paymentMethodSystemName_that_supports_partialrefund";

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        if (ps == PaymentStatus.Paid || order.PaymentStatus == PaymentStatus.PartiallyRefunded)
                        {
                            (await _orderProcessingService.CanPartiallyRefundAsync(order, 10)).ShouldBeTrue();
                        }
                        else
                        {
                            (await _orderProcessingService.CanPartiallyRefundAsync(order, 10)).ShouldBeFalse();
                        }
                    }
                }
            }

            order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_partialrefund";
            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        (await _orderProcessingService.CanPartiallyRefundAsync(order, 10)).ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public async Task Ensure_order_cannot_be_partially_refunded_when_amountToRefund_is_greater_than_amount_that_can_be_refunded()
        {
            var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");

            _paymentServiceMock
                .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_partialrefund", true, 0))
                .ReturnsAsync(testMethod1);

            var order = new Order()
            {
                OrderTotal = 100,
                RefundedAmount = 30, //100-30=70 can be refunded
            };
            order.PaymentMethodSystemName = "paymentMethodSystemName_that_supports_partialrefund";

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        (await _orderProcessingService.CanPartiallyRefundAsync(order, 80)).ShouldBeFalse();
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_can_only_be_partially_refunded_offline_when_paymentstatus_is_paid_or_partiallyRefunded()
        {
            var order = new Order
            {
                OrderTotal = 100
            };

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        if (ps == PaymentStatus.Paid || order.PaymentStatus == PaymentStatus.PartiallyRefunded)
                        {
                            order.CanPartiallyRefundOffline(10).ShouldBeTrue();
                        }
                        else
                        {
                            order.CanPartiallyRefundOffline(10).ShouldBeFalse();
                        }
                    }
                }
            }
        }

        [Test]
        public void Ensure_order_cannot_be_partially_refunded_offline_when_amountToRefund_is_greater_than_amount_that_can_be_refunded()
        {
            var order = new Order()
            {
                OrderTotal = 100,
                RefundedAmount = 30, //100-30=70 can be refunded
            };

            foreach (OrderStatus os in Enum.GetValues(typeof(OrderStatus)))
            {
                foreach (PaymentStatus ps in Enum.GetValues(typeof(PaymentStatus)))
                {
                    foreach (ShippingStatus ss in Enum.GetValues(typeof(ShippingStatus)))
                    {
                        order.OrderStatus = os;
                        order.PaymentStatus = ps;
                        order.ShippingStatus = ss;

                        order.CanPartiallyRefundOffline(80).ShouldBeFalse();
                    }
                }
            }
        }

        //TODO write unit tests for the following methods:
        //PlaceOrder
        //CanCancelRecurringPayment, ProcessNextRecurringPayment, CancelRecurringPayment
    }
}
