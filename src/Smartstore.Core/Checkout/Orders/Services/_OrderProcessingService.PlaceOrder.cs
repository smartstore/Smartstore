using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messages;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
        public virtual async Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest paymentRequest, Dictionary<string, string> extraData)
        {
            Guard.NotNull(paymentRequest, nameof(paymentRequest));

            var result = new PlaceOrderResult();

            try
            {
                if (paymentRequest.OrderGuid == Guid.Empty)
                {
                    paymentRequest.OrderGuid = Guid.NewGuid();
                }

                var initialOrder = await _db.Orders.FindByIdAsync(paymentRequest.InitialOrderId);
                var customer = await _db.Customers.FindByIdAsync(paymentRequest.CustomerId);

                var (warnings, cart) = await ValidateOrderPlacementAsync(paymentRequest, initialOrder, customer);
                if (warnings.Any())
                {
                    result.Errors.AddRange(warnings);
                    Logger.Warn(string.Join(" ", result.Errors));
                    return result;
                }

                if (paymentRequest.IsRecurringPayment)
                {
                    paymentRequest.PaymentMethodSystemName = initialOrder.PaymentMethodSystemName;
                }

                var order = new Order();
                var utcNow = DateTime.UtcNow;

                // Collect data for new order.
                var affiliate = await _db.Affiliates.FindByIdAsync(customer.AffiliateId, false);
                order.AffiliateId = (affiliate?.Active ?? false) ? affiliate.Id : 0;

                await GetCustomerData(order, initialOrder, customer, paymentRequest);

                //...
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                result.Errors.Add(ex.Message);
            }

            if (result.Errors.Any())
            {
                Logger.Error(string.Join(" ", result.Errors));
            }

            return result;
        }

        public virtual async Task<(IList<string> Warnings, IList<OrganizedShoppingCartItem> Cart)> ValidateOrderPlacementAsync(
            ProcessPaymentRequest paymentRequest,
            Order initialOrder = null,
            Customer customer = null)
        {
            Guard.NotNull(paymentRequest, nameof(paymentRequest));

            initialOrder ??= await _db.Orders.FindByIdAsync(paymentRequest.InitialOrderId, false);
            customer ??= await _db.Customers.FindByIdAsync(paymentRequest.CustomerId, false);

            var warnings = new List<string>();
            List<OrganizedShoppingCartItem> cart = null;
            var skipPaymentWorkflow = false;
            var isRecurringCart = false;
            var paymentMethodSystemName = paymentRequest.PaymentMethodSystemName;

            if (customer == null)
            {
                warnings.Add(T("Customer.DoesNotExist"));
                return (warnings, cart);
            }

            // Check whether guest checkout is allowed.
            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                warnings.Add(T("Checkout.AnonymousNotAllowed"));
                return (warnings, cart);
            }

            if (!paymentRequest.IsRecurringPayment)
            {
                cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, paymentRequest.StoreId);

                if (paymentRequest.ShoppingCartItemIds.Any())
                {
                    cart = cart.Where(x => paymentRequest.ShoppingCartItemIds.Contains(x.Item.Id)).ToList();
                }

                if (!cart.Any())
                {
                    warnings.Add(T("ShoppingCart.CartIsEmpty"));
                    return (warnings, cart);
                }


                await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true, customer.GenericAttributes.CheckoutAttributes);
                if (warnings.Any())
                {
                    return (warnings, cart);
                }

                // Validate individual cart items.
                foreach (var item in cart)
                {
                    var ctx = new AddToCartContext
                    {
                        Customer = customer,
                        CartType = item.Item.ShoppingCartType,
                        Product = item.Item.Product,
                        StoreId = paymentRequest.StoreId,
                        RawAttributes = item.Item.RawAttributes,
                        CustomerEnteredPrice = new(item.Item.CustomerEnteredPrice, _primaryCurrency),
                        Quantity = item.Item.Quantity,
                        AutomaticallyAddRequiredProductsIfEnabled = false,
                        ChildItems = item.ChildItems.Select(x => x.Item).ToList()
                    };

                    if (!await _shoppingCartValidator.ValidateAddToCartItemAsync(ctx, cart))
                    {
                        warnings.AddRange(ctx.Warnings);
                        return (warnings, cart);
                    }
                }

                // Order total validation.
                var totalValidation = await ValidateOrderTotalAsync(cart, customer.CustomerRoleMappings.Select(x => x.CustomerRole).ToArray());
                if (!totalValidation.IsAboveMinimum)
                {
                    var convertedMin = _currencyService.ConvertFromPrimaryCurrency(totalValidation.OrderTotalMinimum, _workingCurrency);
                    warnings.Add(T("Checkout.MinOrderSubtotalAmount", convertedMin.ToString(true)));
                }

                if (!totalValidation.IsBelowMaximum)
                {
                    var convertedMax = _currencyService.ConvertFromPrimaryCurrency(totalValidation.OrderTotalMaximum, _workingCurrency);
                    warnings.Add(T("Checkout.MaxOrderSubtotalAmount", convertedMax.ToString(true)));
                }

                if (warnings.Any())
                {
                    return (warnings, cart);
                }

                // Total validations.
                Money? shippingTotalInclTax = await _orderCalculationService.GetShoppingCartShippingTotalAsync(cart, true);
                Money? shippingTotalExclTax = await _orderCalculationService.GetShoppingCartShippingTotalAsync(cart, false);
                if (!shippingTotalInclTax.HasValue || !shippingTotalExclTax.HasValue)
                {
                    warnings.Add(T("Order.CannotCalculateShippingTotal"));
                    return (warnings, cart);
                }

                Money? cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);
                if (!cartTotal.HasValue)
                {
                    warnings.Add(T("Order.CannotCalculateOrderTotal"));
                    return (warnings, cart);
                }

                skipPaymentWorkflow = cartTotal.Value == decimal.Zero;

                // Address validations.
                if (customer.BillingAddress == null)
                {
                    warnings.Add(T("Order.BillingAddressMissing"));
                }
                else if (!customer.BillingAddress.Email.IsEmail())
                {
                    warnings.Add(T("Common.Error.InvalidEmail"));
                }
                else if (customer.BillingAddress.Country != null && !customer.BillingAddress.Country.AllowsBilling)
                {
                    warnings.Add(T("Order.CountryNotAllowedForBilling", customer.BillingAddress.Country.Name));
                }

                if (cart.IsShippingRequired())
                {
                    if (customer.ShippingAddress == null)
                    {
                        warnings.Add(T("Order.ShippingAddressMissing"));
                        throw new SmartException();
                    }
                    else if (!customer.ShippingAddress.Email.IsEmail())
                    {
                        warnings.Add(T("Common.Error.InvalidEmail"));
                    }
                    else if (customer.ShippingAddress.Country != null && !customer.ShippingAddress.Country.AllowsShipping)
                    {
                        warnings.Add(T("Order.CountryNotAllowedForShipping", customer.ShippingAddress.Country.Name));
                    }
                }
            }
            else
            {
                // Recurring order.
                if (initialOrder == null)
                {
                    warnings.Add(T("Order.InitialOrderDoesNotExistForRecurringPayment"));
                    return (warnings, cart);
                }

                var cartTotal = new ShoppingCartTotal
                {
                    Total = new(initialOrder.OrderTotal, _primaryCurrency)
                };

                skipPaymentWorkflow = cartTotal.Total.Value == decimal.Zero;
                paymentMethodSystemName = initialOrder.PaymentMethodSystemName;

                // Address validations.
                if (initialOrder.BillingAddress == null)
                {
                    warnings.Add(T("Order.BillingAddressMissing"));
                }
                else if (initialOrder.BillingAddress.Country != null && !initialOrder.BillingAddress.Country.AllowsBilling)
                {
                    warnings.Add(T("Order.CountryNotAllowedForBilling", initialOrder.BillingAddress.Country.Name));
                }

                if (initialOrder.ShippingStatus != ShippingStatus.ShippingNotRequired)
                {
                    if (initialOrder.ShippingAddress == null)
                    {
                        warnings.Add(T("Order.ShippingAddressMissing"));
                    }
                    else if (initialOrder.ShippingAddress.Country != null && !initialOrder.ShippingAddress.Country.AllowsShipping)
                    {
                        warnings.Add(T("Order.CountryNotAllowedForShipping", initialOrder.ShippingAddress.Country.Name));
                    }
                }
            }

            // Payment.
            if (!warnings.Any() && !skipPaymentWorkflow)
            {
                var isPaymentMethodActive = await _paymentService.IsPaymentMethodActiveAsync(paymentMethodSystemName, customer, cart, paymentRequest.StoreId);
                if (!isPaymentMethodActive)
                {
                    warnings.Add(T("Payment.MethodNotAvailable"));
                }
            }

            // Recurring or standard shopping cart?
            if (!warnings.Any() && !paymentRequest.IsRecurringPayment)
            {
                isRecurringCart = cart.ContainsRecurringItem();
                if (isRecurringCart)
                {
                    var recurringCycleInfo = cart.GetRecurringCycleInfo(_localizationService);
                    if (recurringCycleInfo.ErrorMessage.HasValue())
                    {
                        warnings.Add(recurringCycleInfo.ErrorMessage);
                    }
                }
            }
            else
            {
                isRecurringCart = true;
            }

            // Validate recurring payment type.
            if (!warnings.Any() && !skipPaymentWorkflow && !paymentRequest.IsMultiOrder)
            {
                RecurringPaymentType? recurringPaymentType = isRecurringCart
                    ? await _paymentService.GetRecurringPaymentTypeAsync(paymentRequest.PaymentMethodSystemName)
                    : null;

                if (paymentRequest.IsRecurringPayment && !isRecurringCart)
                {
                    warnings.Add(T("Order.NoRecurringProducts"));
                }

                if (recurringPaymentType.HasValue)
                {
                    switch (recurringPaymentType.Value)
                    {
                        case RecurringPaymentType.NotSupported:
                            warnings.Add(T("Payment.RecurringPaymentNotSupported"));
                            break;
                        case RecurringPaymentType.Manual:
                        case RecurringPaymentType.Automatic:
                            break;
                        default:
                            warnings.Add(T("Payment.RecurringPaymentTypeUnknown"));
                            break;
                    }
                }
            }

            return (warnings, cart);
        }

        public virtual async Task<bool> IsMinimumOrderPlacementIntervalValidAsync(Customer customer, Store store)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(store, nameof(store));

            // Prevent 2 orders being placed within an X seconds time frame.
            if (_orderSettings.MinimumOrderPlacementInterval == 0)
            {
                return true;
            }

            var lastOrder = await _db.Orders
                .AsNoTracking()
                .ApplyStandardFilter(customer.Id, store.Id)
                .FirstOrDefaultAsync();

            if (lastOrder == null)
            {
                return true;
            }

            return (DateTime.UtcNow - lastOrder.CreatedOnUtc).TotalSeconds > _orderSettings.MinimumOrderPlacementInterval;
        }

        #region Utilities

        private async Task GetCustomerData(Order order, Order initialOrder, Customer customer, ProcessPaymentRequest paymentRequest)
        {
            // Customer currency.
            if (!paymentRequest.IsRecurringPayment)
            {
                var customerCurrencyId = customer.GenericAttributes.Get<int>(SystemCustomerAttributeNames.CurrencyId, paymentRequest.StoreId);
                var currencyTmp = await _db.Currencies.FindByIdAsync(customerCurrencyId, false);
                var customerCurrency = (currencyTmp?.Published ?? false) ? currencyTmp : _workingCurrency;

                order.CustomerCurrencyCode = customerCurrency.CurrencyCode;
                order.CurrencyRate = customerCurrency.Rate / _primaryCurrency.Rate;
            }
            else
            {
                order.CustomerCurrencyCode = initialOrder.CustomerCurrencyCode;
                order.CurrencyRate = initialOrder.CurrencyRate;
            }

            // Customer language.
            var languageId = !paymentRequest.IsRecurringPayment
                ? customer.GenericAttributes.Get<int>(SystemCustomerAttributeNames.LanguageId, paymentRequest.StoreId)
                : initialOrder.CustomerLanguageId;

            order.CustomerLanguageId = languageId != 0 ? languageId : _workContext.WorkingLanguage.Id;
        }

        #endregion
    }
}
