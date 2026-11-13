using System;
using System.Collections.Generic;
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

namespace Smartstore.Core.Tests.Checkout.Orders;

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
        var checkoutAttributeMaterializerMock = new Mock<ICheckoutAttributeMaterializer>();
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
            checkoutAttributeMaterializerMock.Object,
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
    public void Order_can_be_cancelled_if_not_already_cancelled()
    {
        var order = new Order();
        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.OrderStatus != OrderStatus.Cancelled)
            {
                currentOrder.CanCancelOrder().ShouldBeTrue();
            }
            else
            {
                currentOrder.CanCancelOrder().ShouldBeFalse();
            }
        }
    }

    [Test]
    public void Order_can_be_authorized_if_pending_and_not_cancelled()
    {
        var order = new Order();
        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.OrderStatus != OrderStatus.Cancelled && currentOrder.PaymentStatus == PaymentStatus.Pending)
            {
                currentOrder.CanMarkOrderAsAuthorized().ShouldBeTrue();
            }
            else
            {
                currentOrder.CanMarkOrderAsAuthorized().ShouldBeFalse();
            }
        }
    }

    [Test]
    public async Task Order_can_be_captured_if_authorized_and_provider_supports_capture()
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

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.OrderStatus != OrderStatus.Cancelled &&
                currentOrder.OrderStatus != OrderStatus.Pending &&
                currentOrder.PaymentStatus == PaymentStatus.Authorized)
            {
                (await _orderProcessingService.CanCaptureAsync(currentOrder)).ShouldBeTrue();
            }
            else
            {
                (await _orderProcessingService.CanCaptureAsync(currentOrder)).ShouldBeFalse();
            }
        }

        order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_capture";

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            (await _orderProcessingService.CanCaptureAsync(currentOrder)).ShouldBeFalse();
        }
    }

    [Test]
    public void Order_cannot_be_paid_if_cancelled_paid_or_refunded()
    {
        var order = new Order();
        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.OrderStatus == OrderStatus.Cancelled ||
                currentOrder.PaymentStatus == PaymentStatus.Paid ||
                currentOrder.PaymentStatus == PaymentStatus.Refunded)
            {
                currentOrder.CanMarkOrderAsPaid().ShouldBeFalse();
            }
            else
            {
                currentOrder.CanMarkOrderAsPaid().ShouldBeTrue();
            }
        }
    }

    [Test]
    public async Task Order_can_be_refunded_if_paid_and_provider_supports_refund()
    {
        var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_refund", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod1);

        var testMethod2 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod2");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_doesn't_support_refund", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod2);

        var order = new Order
        {
            OrderTotal = 1,
            PaymentMethodSystemName = "paymentMethodSystemName_that_supports_refund"
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.PaymentStatus == PaymentStatus.Paid)
            {
                (await _orderProcessingService.CanRefundAsync(currentOrder)).ShouldBeTrue();
            }
            else
            {
                (await _orderProcessingService.CanRefundAsync(currentOrder)).ShouldBeFalse();
            }
        }

        order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_refund";
        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            (await _orderProcessingService.CanRefundAsync(currentOrder)).ShouldBeFalse();
        }
    }

    [Test]
    public async Task Order_cannot_be_refunded_if_total_is_zero()
    {
        var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_refund", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod1);

        var order = new Order
        {
            PaymentMethodSystemName = "paymentMethodSystemName_that_supports_refund"
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            (await _orderProcessingService.CanRefundAsync(currentOrder)).ShouldBeFalse();
        }
    }

    [Test]
    public void Order_can_be_refunded_offline_if_paid()
    {
        var order = new Order()
        {
            OrderTotal = 1,
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.PaymentStatus == PaymentStatus.Paid)
                currentOrder.CanRefundOffline().ShouldBeTrue();
            else
                currentOrder.CanRefundOffline().ShouldBeFalse();
        }
    }

    [Test]
    public void Order_cannot_be_refunded_offline_if_total_is_zero()
    {
        var order = new Order();

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            currentOrder.CanRefundOffline().ShouldBeFalse();
        }
    }

    [Test]
    public async Task Order_can_be_voided_if_authorized_and_provider_supports_void()
    {
        var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_void", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod1);

        var testMethod2 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod2");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_doesn't_support_void", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod2);

        var order = new Order
        {
            OrderTotal = 1,
            PaymentMethodSystemName = "paymentMethodSystemName_that_supports_void"
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.PaymentStatus == PaymentStatus.Authorized)
            {
                (await _orderProcessingService.CanVoidAsync(currentOrder)).ShouldBeTrue();
            }
            else
            {
                (await _orderProcessingService.CanVoidAsync(currentOrder)).ShouldBeFalse();
            }
        }

        order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_void";
        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            (await _orderProcessingService.CanVoidAsync(currentOrder)).ShouldBeFalse();
        }
    }

    [Test]
    public async Task Order_cannot_be_voided_if_total_is_zero()
    {
        var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_void", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod1);

        var order = new Order
        {
            PaymentMethodSystemName = "paymentMethodSystemName_that_supports_void"
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            (await _orderProcessingService.CanVoidAsync(currentOrder)).ShouldBeFalse();
        }
    }

    [Test]
    public void Order_can_be_voided_offline_if_authorized()
    {
        var order = new Order()
        {
            OrderTotal = 1,
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.PaymentStatus == PaymentStatus.Authorized)
            {
                currentOrder.CanVoidOffline().ShouldBeTrue();
            }
            else
            {
                currentOrder.CanVoidOffline().ShouldBeFalse();
            }
        }
    }

    [Test]
    public void Order_cannot_be_voided_offline_if_total_is_zero()
    {
        var order = new Order();

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            currentOrder.CanVoidOffline().ShouldBeFalse();
        }
    }

    [Test]
    public async Task Order_can_be_partially_refunded_if_paid_and_provider_supports_partial_refund()
    {
        var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_partialrefund", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod1);

        var testMethod2 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod2");
        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_doesn't_support_partialrefund", It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(testMethod2);

        var order = new Order
        {
            OrderTotal = 100,
            PaymentMethodSystemName = "paymentMethodSystemName_that_supports_partialrefund"
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.PaymentStatus is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded)
            {
                (await _orderProcessingService.CanPartiallyRefundAsync(currentOrder, 10)).ShouldBeTrue();
            }
            else
            {
                (await _orderProcessingService.CanPartiallyRefundAsync(currentOrder, 10)).ShouldBeFalse();
            }
        }

        order.PaymentMethodSystemName = "paymentMethodSystemName_that_doesn't_support_partialrefund";

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            (await _orderProcessingService.CanPartiallyRefundAsync(currentOrder, 10)).ShouldBeFalse();
        }
    }

    [Test]
    public async Task Order_cannot_be_partially_refunded_if_amount_exceeds_refundable_amount()
    {
        var testMethod1 = ProviderManager.GetProvider<IPaymentMethod>("Payments.TestMethod1");

        _paymentServiceMock
            .Setup(x => x.LoadPaymentProviderBySystemNameAsync("paymentMethodSystemName_that_supports_partialrefund", true, 0))
            .ReturnsAsync(testMethod1);

        var order = new Order
        {
            OrderTotal = 100,
            RefundedAmount = 30, //100-30=70 can be refunded
            PaymentMethodSystemName = "paymentMethodSystemName_that_supports_partialrefund"
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            (await _orderProcessingService.CanPartiallyRefundAsync(currentOrder, 80)).ShouldBeFalse();
        }
    }

    [Test]
    public void Order_can_be_partially_refunded_offline_if_paid()
    {
        var order = new Order
        {
            OrderTotal = 100
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            if (currentOrder.PaymentStatus is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded)
            {
                currentOrder.CanPartiallyRefundOffline(10).ShouldBeTrue();
            }
            else
            {
                currentOrder.CanPartiallyRefundOffline(10).ShouldBeFalse();
            }
        }
    }

    [Test]
    public void Order_cannot_be_partially_refunded_offline_if_amount_exceeds_refundable_amount()
    {
        var order = new Order()
        {
            OrderTotal = 100,
            RefundedAmount = 30, //100-30=70 can be refunded
        };

        foreach (var currentOrder in GetAllStatusCombinations(order))
        {
            currentOrder.CanPartiallyRefundOffline(80).ShouldBeFalse();
        }
    }

    [TestCase(3, 2, 8.0, 6.0)]
    [TestCase(2, 3, 18.0, 13.5)]
    [TestCase(4, 1, 3.0, 2.25)]
    [TestCase(0, 2, 12.0, 9.0)]
    public async Task Order_item_discounts_scale_with_quantity(
        int oldQuantity,
        int newQuantity,
        double expectedDiscountInclTax,
        double expectedDiscountExclTax)
    {
        _currency.RoundOrderItemsEnabled = true;

        var order = new Order
        {
            Customer = new Customer(),
            CustomerCurrencyCode = "USD",
            OrderStatus = OrderStatus.Pending,
            OrderSubtotalInclTax = oldQuantity * 10M,
            OrderSubtotalExclTax = oldQuantity * 10M,
            OrderTotal = oldQuantity * 10M
        };
        var orderItem = new OrderItem
        {
            Order = order,
            Quantity = oldQuantity,
            UnitPriceInclTax = 10M,
            UnitPriceExclTax = 10M,
            PriceInclTax = oldQuantity * 10M,
            PriceExclTax = oldQuantity * 10M,
            DiscountAmountInclTax = 12M,
            DiscountAmountExclTax = 9M
        };
        order.OrderItems.Add(orderItem);

        var context = new UpdateOrderDetailsContext
        {
            OldQuantity = oldQuantity,
            NewQuantity = newQuantity,
            UpdateTotals = true
        };

        await _orderProcessingService.UpdateOrderDetailsAsync(orderItem, context);

        orderItem.DiscountAmountInclTax.ShouldEqual(Convert.ToDecimal(expectedDiscountInclTax));
        orderItem.DiscountAmountExclTax.ShouldEqual(Convert.ToDecimal(expectedDiscountExclTax));
    }

    private static IEnumerable<Order> GetAllStatusCombinations(Order order)
    {
        foreach (var orderStatus in Enum.GetValues<OrderStatus>())
        {
            foreach (var paymentStatus in Enum.GetValues<PaymentStatus>())
            {
                foreach (var shippingStatus in Enum.GetValues<ShippingStatus>())
                {
                    order.OrderStatus = orderStatus;
                    order.PaymentStatus = paymentStatus;
                    order.ShippingStatus = shippingStatus;

                    yield return order;
                }
            }
        }
    }
}