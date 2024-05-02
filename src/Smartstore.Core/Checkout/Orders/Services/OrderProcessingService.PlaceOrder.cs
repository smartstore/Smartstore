using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Stores;
using Smartstore.Utilities;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
        public virtual async Task<OrderPlacementResult> PlaceOrderAsync(ProcessPaymentRequest paymentRequest, Dictionary<string, string> extraData)
        {
            Guard.NotNull(paymentRequest);

            extraData ??= [];

            if (paymentRequest.OrderGuid == Guid.Empty)
            {
                paymentRequest.OrderGuid = Guid.NewGuid();
            }

            var ctx = new PlaceOrderContext
            {
                InitialOrder = await _db.Orders.FindByIdAsync(paymentRequest.InitialOrderId),
                Customer = await _db.Customers
                    .IncludeCustomerRoles()
                    .FindByIdAsync(paymentRequest.CustomerId),
                ExtraData = extraData,
                PaymentRequest = paymentRequest
            };

            if (!paymentRequest.IsRecurringPayment)
            {
                ctx.Cart = await _shoppingCartService.GetCartAsync(ctx.Customer, ShoppingCartType.ShoppingCart, paymentRequest.StoreId);
                ctx.BatchContext = _productService.CreateProductBatchContext(ctx.Cart.GetAllProducts(), null, ctx.Customer, false);
                ctx.CartRequiresShipping = ctx.Cart.IsShippingRequired;
            }
            else
            {
                ctx.CartRequiresShipping = ctx.InitialOrder.ShippingStatus != ShippingStatus.ShippingNotRequired;
                paymentRequest.PaymentMethodSystemName = ctx.InitialOrder.PaymentMethodSystemName;
            }

            var (warnings, _) = await ValidateOrderPlacementInternal(paymentRequest, ctx.InitialOrder, ctx.Customer, ctx.BatchContext);
            if (warnings.Count > 0)
            {
                ctx.Result.Errors.AddRange(warnings);
                return ctx.Result;
            }

            // Collect data for new order.
            // Also applies data (like order and tax total) to paymentRequest for payment processing below.
            await ApplyCustomerData(ctx);
            await ApplyPricingData(ctx);

            await ProcessPayment(ctx);

            _db.Orders.Add(ctx.Order);

            // Save, we need the primary key.
            // Payment has been made. Order MUST be saved immediately!
            await _db.SaveChangesAsync();

            ctx.Result.PlacedOrder = ctx.Order;

            // Also applies data (like discounts) required for saving associated data.
            await AddOrderItems(ctx);
            await AddAssociatedData(ctx);

            // Save order items.
            await _db.SaveChangesAsync();

            // Email messages, order notes etc.
            await FinalizeOrderPlacement(ctx);

            // Saves changes to database.
            await CheckOrderStatusAsync(ctx.Order);

            // Events.
            await _eventPublisher.PublishOrderPlacedAsync(ctx.Order);

            if (ctx.Order.PaymentStatus == PaymentStatus.Paid)
            {
                await _eventPublisher.PublishOrderPaidAsync(ctx.Order);
            }

            return ctx.Result;
        }

        public virtual Task<(IList<string> Warnings, ShoppingCart Cart)> ValidateOrderPlacementAsync(
            ProcessPaymentRequest paymentRequest,
            Order initialOrder = null,
            Customer customer = null)
        {
            return ValidateOrderPlacementInternal(paymentRequest, initialOrder, customer, null);
        }

        private async Task<(IList<string> Warnings, ShoppingCart Cart)> ValidateOrderPlacementInternal(
            ProcessPaymentRequest paymentRequest,
            Order initialOrder,
            Customer customer,
            ProductBatchContext batchContext)
        {
            Guard.NotNull(paymentRequest);

            initialOrder ??= await _db.Orders.FindByIdAsync(paymentRequest.InitialOrderId);

            customer ??= await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(paymentRequest.CustomerId);

            var warnings = new List<string>();
            ShoppingCart cart = null;
            var paymentRequired = true;
            var isRecurringCart = false;
            var paymentSystemName = paymentRequest.PaymentMethodSystemName;

            if (customer == null)
            {
                warnings.Add(T("Customer.DoesNotExist"));
                return (warnings, cart);
            }

            // Check whether guest checkout is allowed.
            if (!_orderSettings.AnonymousCheckoutAllowed && !customer.IsRegistered())
            {
                warnings.Add(T("Checkout.AnonymousNotAllowed"));
                return (warnings, cart);
            }

            if (!paymentRequest.IsRecurringPayment)
            {
                cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, paymentRequest.StoreId);

                if (paymentRequest.ShoppingCartItemIds.Count > 0)
                {
                    cart = new ShoppingCart(cart, cart.Items.Where(x => paymentRequest.ShoppingCartItemIds.Contains(x.Item.Id)));
                }

                if (!cart.HasItems)
                {
                    warnings.Add(T("ShoppingCart.CartIsEmpty"));
                    return (warnings, cart);
                }

                await _shoppingCartValidator.ValidateCartAsync(cart, warnings, true);
                if (warnings.Count > 0)
                {
                    return (warnings, cart);
                }

                // Validate individual cart items.
                foreach (var item in cart.Items)
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
                        AutomaticallyAddRequiredProducts = false,
                        ChildItems = item.ChildItems.Select(x => x.Item).ToList()
                    };

                    if (!await _shoppingCartValidator.ValidateAddToCartItemAsync(ctx, item.Item, cart.Items))
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

                if (warnings.Count > 0)
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

                Money? cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart, batchContext: batchContext);
                if (!cartTotal.HasValue)
                {
                    warnings.Add(T("Order.CannotCalculateOrderTotal"));
                    return (warnings, cart);
                }

                paymentRequired = cartTotal.Value != decimal.Zero && cart.Requirements.HasFlag(CheckoutRequirements.Payment);

                // Address validations.
                if (cart.Requirements.HasFlag(CheckoutRequirements.BillingAddress))
                {
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
                        warnings.Add(T("Order.CountryNotAllowedForBilling", customer.BillingAddress.Country.GetLocalized(x => x.Name)));
                    }
                }

                if (cart.IsShippingRequired)
                {
                    if (customer.ShippingAddress == null)
                    {
                        warnings.Add(T("Order.ShippingAddressMissing"));
                        throw new Exception();
                    }
                    else if (!customer.ShippingAddress.Email.IsEmail())
                    {
                        warnings.Add(T("Common.Error.InvalidEmail"));
                    }
                    else if (customer.ShippingAddress.Country != null && !customer.ShippingAddress.Country.AllowsShipping)
                    {
                        warnings.Add(T("Order.CountryNotAllowedForShipping", customer.ShippingAddress.Country.GetLocalized(x => x.Name)));
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

                paymentRequired = cartTotal.Total.Value != decimal.Zero && cart.Requirements.HasFlag(CheckoutRequirements.Payment);
                paymentSystemName = initialOrder.PaymentMethodSystemName;

                // Address validations.
                if (initialOrder.BillingAddress?.Country != null && !initialOrder.BillingAddress.Country.AllowsBilling)
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
            if (warnings.Count == 0 
                && paymentRequired
                && (paymentSystemName.IsEmpty() || !await _paymentService.IsPaymentProviderActiveAsync(paymentSystemName, cart, paymentRequest.StoreId)))
            {
                warnings.Add(T("Payment.MethodNotAvailable"));
            }

            if (warnings.Count == 0 && !paymentRequest.IsRecurringPayment)
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
            if (warnings.Count == 0 && paymentRequired && !paymentRequest.IsMultiOrder)
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
            Guard.NotNull(customer);
            Guard.NotNull(store);

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

        private async Task ApplyCustomerData(PlaceOrderContext ctx)
        {
            var order = ctx.Order;
            var customer = ctx.Customer;
            var affiliate = await _db.Affiliates.FindByIdAsync(customer.AffiliateId, false);

            order.CustomerId = customer.Id;
            order.CustomerIp = _webHelper.GetClientIpAddress().ToString();
            order.AffiliateId = (affiliate?.Active ?? false) ? affiliate.Id : 0;
            order.ShippingStatus = ctx.CartRequiresShipping ? ShippingStatus.NotYetShipped : ShippingStatus.ShippingNotRequired;

            if (!ctx.PaymentRequest.IsRecurringPayment)
            {
                var storeId = ctx.PaymentRequest.StoreId;
                var customerCurrencyId = customer.GenericAttributes.Get<int>(SystemCustomerAttributeNames.CurrencyId, storeId);
                var currencyTmp = await _db.Currencies.FindByIdAsync(customerCurrencyId, false);
                var customerCurrency = (currencyTmp?.Published ?? false) ? currencyTmp : _workingCurrency;

                order.CustomerCurrencyCode = customerCurrency.CurrencyCode;
                order.CurrencyRate = customerCurrency.Rate / _primaryCurrency.Rate;
                order.CustomerLanguageId = customer.GenericAttributes.Get<int>(SystemCustomerAttributeNames.LanguageId, storeId);
                order.CustomerTaxDisplayType = await _workContext.GetTaxDisplayTypeAsync(customer, storeId);

                order.VatNumber = _taxSettings.EuVatEnabled && (VatNumberStatus)customer.VatNumberStatusId == VatNumberStatus.Valid
                    ? customer.GenericAttributes.VatNumber
                    : string.Empty;

                order.RawAttributes = customer.GenericAttributes.RawCheckoutAttributes;
                order.CheckoutAttributeDescription = await _checkoutAttributeFormatter.FormatAttributesAsync(customer.GenericAttributes.CheckoutAttributes, customer);

                if (ctx.CartRequiresShipping)
                {
                    var shippingOption = customer.GenericAttributes.Get<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, storeId);
                    if (shippingOption != null)
                    {
                        order.ShippingMethod = shippingOption.Name;
                        order.ShippingRateComputationMethodSystemName = shippingOption.ShippingRateComputationMethodSystemName;
                    }
                }
            }
            else
            {
                var io = ctx.InitialOrder;

                order.CustomerCurrencyCode = io.CustomerCurrencyCode;
                order.CurrencyRate = io.CurrencyRate;
                order.CustomerLanguageId = io.CustomerLanguageId;
                order.CustomerTaxDisplayType = io.CustomerTaxDisplayType;
                order.VatNumber = io.VatNumber;
                order.RawAttributes = io.RawAttributes;
                order.CheckoutAttributeDescription = io.CheckoutAttributeDescription;

                if (ctx.CartRequiresShipping)
                {
                    order.ShippingMethod = io.ShippingMethod;
                    order.ShippingRateComputationMethodSystemName = io.ShippingRateComputationMethodSystemName;
                }
            }

            if (order.CustomerLanguageId == 0)
            {
                order.CustomerLanguageId = _workContext.WorkingLanguage.Id;
            }

            // Apply extra data.
            if (ctx.ExtraData.TryGetValue("CustomerComment", out var customerComment))
            {
                order.CustomerOrderComment = customerComment;
            }

            if (_shoppingCartSettings.ThirdPartyEmailHandOver != CheckoutThirdPartyEmailHandOver.None &&
                ctx.ExtraData.TryGetValue("AcceptThirdPartyEmailHandOver", out var acceptEmailHandOver))
            {
                order.AcceptThirdPartyEmailHandOver = acceptEmailHandOver.ToBool();
            }
        }

        private async Task ApplyPricingData(PlaceOrderContext ctx)
        {
            var order = ctx.Order;

            if (!ctx.PaymentRequest.IsRecurringPayment)
            {
                // Sub total.
                var subTotalInclTax = await _orderCalculationService.GetShoppingCartSubtotalAsync(ctx.Cart, true, batchContext: ctx.BatchContext);
                var subTotalExclTax = await _orderCalculationService.GetShoppingCartSubtotalAsync(ctx.Cart, false, batchContext: ctx.BatchContext);

                order.OrderSubtotalInclTax = subTotalInclTax.SubtotalWithoutDiscount.Amount;
                order.OrderSubtotalExclTax = subTotalExclTax.SubtotalWithoutDiscount.Amount;
                order.OrderSubTotalDiscountInclTax = subTotalInclTax.DiscountAmount.Amount;
                order.OrderSubTotalDiscountExclTax = subTotalExclTax.DiscountAmount.Amount;

                ctx.AddDiscount(subTotalInclTax.AppliedDiscount);

                // Shipping total.
                var shippingTotalInclTax = await _orderCalculationService.GetShoppingCartShippingTotalAsync(ctx.Cart, true);
                var shippingTotalExclTax = await _orderCalculationService.GetShoppingCartShippingTotalAsync(ctx.Cart, false);

                order.OrderShippingInclTax = shippingTotalInclTax.ShippingTotal?.Amount ?? decimal.Zero;
                order.OrderShippingExclTax = shippingTotalExclTax.ShippingTotal?.Amount ?? decimal.Zero;
                order.OrderShippingTaxRate = shippingTotalInclTax.TaxRate;

                ctx.AddDiscount(shippingTotalInclTax.AppliedDiscount);

                // Payment total.
                var paymentFee = await _orderCalculationService.GetShoppingCartPaymentFeeAsync(ctx.Cart, ctx.PaymentRequest.PaymentMethodSystemName);
                var paymentFeeTax = await _taxCalculator.CalculatePaymentFeeTaxAsync(paymentFee.Amount, customer: ctx.Customer);

                order.PaymentMethodAdditionalFeeInclTax = paymentFeeTax.PriceGross;
                order.PaymentMethodAdditionalFeeExclTax = paymentFeeTax.PriceNet;
                order.PaymentMethodAdditionalFeeTaxRate = paymentFeeTax.Rate.Rate;

                // Tax total.
                var (taxTotal, taxRates) = await _orderCalculationService.GetShoppingCartTaxTotalAsync(ctx.Cart);
                order.OrderTax = taxTotal.Amount;
                order.TaxRates = FormatTaxRates(taxRates);

                // Order total.
                ctx.CartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(ctx.Cart, batchContext: ctx.BatchContext);
                order.OrderTotal = ctx.CartTotal.Total.Value.Amount;
                order.OrderTotalRounding = ctx.CartTotal.ToNearestRounding.Amount;
                order.RefundedAmount = decimal.Zero;
                order.OrderDiscount = ctx.CartTotal.DiscountAmount.Amount;
                order.CreditBalance = ctx.CartTotal.CreditBalance.Amount;

                ctx.AddDiscount(ctx.CartTotal.AppliedDiscount);
            }
            else
            {
                var io = ctx.InitialOrder;

                ctx.CartTotal = new ShoppingCartTotal
                {
                    Total = new(io.OrderTotal, _primaryCurrency),
                    DiscountAmount = new(io.OrderDiscount, _primaryCurrency)
                };

                order.OrderSubtotalInclTax = io.OrderSubtotalInclTax;
                order.OrderSubtotalExclTax = io.OrderSubtotalExclTax;
                order.OrderSubTotalDiscountInclTax = io.OrderSubTotalDiscountInclTax;
                order.OrderSubTotalDiscountExclTax = io.OrderSubTotalDiscountExclTax;
                order.OrderShippingInclTax = io.OrderShippingInclTax;
                order.OrderShippingExclTax = io.OrderShippingExclTax;
                order.OrderShippingTaxRate = io.OrderShippingTaxRate;
                order.PaymentMethodAdditionalFeeInclTax = io.PaymentMethodAdditionalFeeInclTax;
                order.PaymentMethodAdditionalFeeExclTax = io.PaymentMethodAdditionalFeeExclTax;
                order.PaymentMethodAdditionalFeeTaxRate = io.PaymentMethodAdditionalFeeTaxRate;
                order.OrderTax = io.OrderTax;
                order.TaxRates = io.TaxRates;
                order.OrderTotal = io.OrderTotal;
                order.OrderTotalRounding = io.OrderTotalRounding;
                order.RefundedAmount = decimal.Zero;
                order.OrderDiscount = io.OrderDiscount;
                order.CreditBalance = io.CreditBalance;
            }

            ctx.PaymentRequest.OrderTax = order.OrderTax;
            ctx.PaymentRequest.OrderTotal = ctx.CartTotal.Total.Value.Amount;
        }

        private async Task ProcessPayment(PlaceOrderContext ctx)
        {
            var result = new ProcessPaymentResult();
            var order = ctx.Order;
            var io = ctx.InitialOrder;
            var pr = ctx.PaymentRequest;
            var billingAddressRequired = ctx.Cart.Requirements.HasFlag(CheckoutRequirements.BillingAddress);
            var paymentRequired = ctx.CartTotal.Total.Value != decimal.Zero && ctx.Cart.Requirements.HasFlag(CheckoutRequirements.Payment);

            if (paymentRequired)
            {
                // Give payment processor the opportunity to fullfill billing address.
                await _paymentService.PreProcessPaymentAsync(pr);
            }
            else
            {
                pr.PaymentMethodSystemName = string.Empty;
            }

            if (!pr.IsRecurringPayment)
            {
                order.BillingAddress = billingAddressRequired ? (Address)ctx.Customer.BillingAddress?.Clone() : null;
                order.ShippingAddress = ctx.CartRequiresShipping ? (Address)ctx.Customer.ShippingAddress?.Clone() : null;

                ctx.IsRecurringCart = ctx.Cart.ContainsRecurringItem();
                if (ctx.IsRecurringCart)
                {
                    var cycleInfo = ctx.Cart.GetRecurringCycleInfo(_localizationService);
                    pr.RecurringCycleLength = cycleInfo.CycleLength ?? 0;
                    pr.RecurringCyclePeriod = cycleInfo.CyclePeriod ?? RecurringProductCyclePeriod.Days;
                    pr.RecurringTotalCycles = cycleInfo.TotalCycles ?? 0;
                }
            }
            else
            {
                order.BillingAddress = billingAddressRequired ? (Address)io.BillingAddress?.Clone() : null;
                order.ShippingAddress = ctx.CartRequiresShipping ? (Address)io.ShippingAddress?.Clone() : null;
                
                ctx.IsRecurringCart = true;
            }

            // Process payment.
            if (paymentRequired && !pr.IsMultiOrder)
            {
                if (!pr.IsRecurringPayment)
                {
                    if (!ctx.IsRecurringCart)
                    {
                        result = await _paymentService.ProcessPaymentAsync(pr);
                    }
                    else
                    {
                        var recurringPaymentType = await _paymentService.GetRecurringPaymentTypeAsync(pr.PaymentMethodSystemName);
                        switch (recurringPaymentType)
                        {
                            case RecurringPaymentType.Manual:
                            case RecurringPaymentType.Automatic:
                                result = await _paymentService.ProcessRecurringPaymentAsync(pr);
                                break;
                            case RecurringPaymentType.NotSupported:
                                throw new PaymentException(T("Payment.RecurringPaymentNotSupported"));
                            default:
                                throw new PaymentException(T("Payment.RecurringPaymentTypeUnknown"));
                        }
                    }
                }
                else
                {
                    if (ctx.IsRecurringCart)
                    {
                        // Old credit card info.
                        pr.CreditCardType = io.AllowStoringCreditCardNumber ? _encryptor.DecryptText(io.CardType) : string.Empty;
                        pr.CreditCardName = io.AllowStoringCreditCardNumber ? _encryptor.DecryptText(io.CardName) : string.Empty;
                        pr.CreditCardNumber = io.AllowStoringCreditCardNumber ? _encryptor.DecryptText(io.CardNumber) : string.Empty;
                        pr.CreditCardCvv2 = io.AllowStoringCreditCardNumber ? _encryptor.DecryptText(io.CardCvv2) : string.Empty;
                        pr.CreditCardExpireMonth = io.AllowStoringCreditCardNumber ? _encryptor.DecryptText(io.CardExpirationMonth).ToInt() : 0;
                        pr.CreditCardExpireYear = io.AllowStoringCreditCardNumber ? _encryptor.DecryptText(io.CardExpirationYear).ToInt() : 0;

                        var recurringPaymentType = await _paymentService.GetRecurringPaymentTypeAsync(pr.PaymentMethodSystemName);
                        switch (recurringPaymentType)
                        {
                            case RecurringPaymentType.Manual:
                                result = await _paymentService.ProcessRecurringPaymentAsync(pr);
                                break;
                            case RecurringPaymentType.Automatic:
                                // Payment is processed on payment gateway site.
                                break;
                            case RecurringPaymentType.NotSupported:
                                throw new PaymentException(T("Payment.RecurringPaymentNotSupported"));
                            default:
                                throw new PaymentException(T("Payment.RecurringPaymentTypeUnknown"));
                        }
                    }
                    else
                    {
                        throw new PaymentException(T("Order.NoRecurringProducts"));
                    }
                }
            }
            else
            {
                result.NewPaymentStatus = PaymentStatus.Paid;
            }

            order.StoreId = pr.StoreId;
            order.OrderGuid = pr.OrderGuid;
            order.OrderStatus = OrderStatus.Pending;
            order.AllowStoringCreditCardNumber = result.AllowStoringCreditCardNumber;
            order.CardType = result.AllowStoringCreditCardNumber ? _encryptor.EncryptText(pr.CreditCardType) : string.Empty;
            order.CardName = result.AllowStoringCreditCardNumber ? _encryptor.EncryptText(pr.CreditCardName) : string.Empty;
            order.CardNumber = result.AllowStoringCreditCardNumber ? _encryptor.EncryptText(pr.CreditCardNumber) : string.Empty;
            order.MaskedCreditCardNumber = _encryptor.EncryptText(_paymentService.GetMaskedCreditCardNumber(pr.CreditCardNumber));
            order.CardCvv2 = result.AllowStoringCreditCardNumber ? _encryptor.EncryptText(pr.CreditCardCvv2) : string.Empty;
            order.CardExpirationMonth = result.AllowStoringCreditCardNumber ? _encryptor.EncryptText(pr.CreditCardExpireMonth.ToString()) : string.Empty;
            order.CardExpirationYear = result.AllowStoringCreditCardNumber ? _encryptor.EncryptText(pr.CreditCardExpireYear.ToString()) : string.Empty;
            order.AllowStoringDirectDebit = result.AllowStoringDirectDebit;
            order.DirectDebitAccountHolder = result.AllowStoringDirectDebit ? _encryptor.EncryptText(pr.DirectDebitAccountHolder) : string.Empty;
            order.DirectDebitAccountNumber = result.AllowStoringDirectDebit ? _encryptor.EncryptText(pr.DirectDebitAccountNumber) : string.Empty;
            order.DirectDebitBankCode = result.AllowStoringDirectDebit ? _encryptor.EncryptText(pr.DirectDebitBankCode) : string.Empty;
            order.DirectDebitBankName = result.AllowStoringDirectDebit ? _encryptor.EncryptText(pr.DirectDebitBankName) : string.Empty;
            order.DirectDebitBIC = result.AllowStoringDirectDebit ? _encryptor.EncryptText(pr.DirectDebitBic) : string.Empty;
            order.DirectDebitCountry = result.AllowStoringDirectDebit ? _encryptor.EncryptText(pr.DirectDebitCountry) : string.Empty;
            order.DirectDebitIban = result.AllowStoringDirectDebit ? _encryptor.EncryptText(pr.DirectDebitIban) : string.Empty;
            order.PaymentMethodSystemName = pr.PaymentMethodSystemName;
            order.AuthorizationTransactionId = result.AuthorizationTransactionId;
            order.AuthorizationTransactionCode = result.AuthorizationTransactionCode;
            order.AuthorizationTransactionResult = result.AuthorizationTransactionResult;
            order.CaptureTransactionId = result.CaptureTransactionId;
            order.CaptureTransactionResult = result.CaptureTransactionResult.Truncate(400);
            order.SubscriptionTransactionId = result.SubscriptionTransactionId;
            order.PurchaseOrderNumber = pr.PurchaseOrderNumber;
            order.PaymentStatus = result.NewPaymentStatus;
            order.PaidDateUtc = result.NewPaymentStatus == PaymentStatus.Paid ? ctx.Now : null;
        }

        private async Task AddOrderItems(PlaceOrderContext ctx)
        {
            if (!ctx.PaymentRequest.IsRecurringPayment)
            {
                var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, ctx.Customer, _primaryCurrency, ctx.BatchContext);

                foreach (var cartItem in ctx.Cart.Items)
                {
                    var item = cartItem.Item;
                    var product = item.Product;

                    await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

                    var attributeDescription = await _productAttributeFormatter.FormatAttributesAsync(
                        item.AttributeSelection, 
                        product,
                        ProductAttributeFormatOptions.Default,
                        ctx.Customer);

                    var itemWeight = await _shippingService.GetCartItemWeightAsync(cartItem, false);
                    var displayDeliveryTime =
                        _shoppingCartSettings.DeliveryTimesInShoppingCart != DeliveryTimesPresentation.None &&
                        product.DeliveryTimeId.HasValue &&
                        product.IsShippingEnabled &&
                        product.DisplayDeliveryTimeAccordingToStock(_catalogSettings);

                    // Product cost always in primary currency without tax.
                    var productCost = await _priceCalculationService.CalculateProductCostAsync(product, item.AttributeSelection);

                    var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(cartItem, calculationOptions);
                    var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);
                    var discountTax = await _taxCalculator.CalculateProductTaxAsync(product, subtotal.DiscountAmount.Amount, null, ctx.Customer);

                    subtotal.AppliedDiscounts.Each(ctx.AddDiscount);

                    var orderItem = new OrderItem
                    {
                        OrderItemGuid = Guid.NewGuid(),
                        Order = ctx.Order,
                        ProductId = item.ProductId,
                        Sku = product.Sku,
                        UnitPriceInclTax = unitPrice.Tax.Value.PriceGross,
                        UnitPriceExclTax = unitPrice.Tax.Value.PriceNet,
                        PriceInclTax = subtotal.Tax.Value.PriceGross,
                        PriceExclTax = subtotal.Tax.Value.PriceNet,
                        TaxRate = unitPrice.Tax.Value.Rate.Rate,
                        DiscountAmountInclTax = discountTax.PriceGross,
                        DiscountAmountExclTax = discountTax.PriceNet,
                        AttributeDescription = attributeDescription,
                        RawAttributes = item.RawAttributes,
                        Quantity = item.Quantity,
                        DownloadCount = 0,
                        IsDownloadActivated = false,
                        LicenseDownloadId = 0,
                        ItemWeight = itemWeight,
                        ProductCost = productCost.Amount,
                        DeliveryTimeId = product.GetDeliveryTimeIdAccordingToStock(_catalogSettings),
                        DisplayDeliveryTime = displayDeliveryTime
                    };

                    if (product.ProductType == ProductType.BundledProduct && cartItem.ChildItems != null)
                    {
                        var bundleItemDataList = new List<ProductBundleItemOrderData>();
                        var childItems = cartItem.ChildItems.Where(x => x.Item.ProductId != 0 && x.Item.BundleItemId != 0).ToArray();

                        foreach (var childItem in childItems)
                        {
                            var childCalculationContext = await _priceCalculationService.CreateCalculationContextAsync(childItem, calculationOptions);
                            var (_, childSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(childCalculationContext);

                            var attributesInfo = await _productAttributeFormatter.FormatAttributesAsync(
                                childItem.Item.AttributeSelection,
                                childItem.Item.Product,
                                new() { IncludePrices = false },
                                ctx.Customer);

                            var bundleItemData = childItem.Item.BundleItem.ToOrderData(childSubtotal.FinalPrice.Amount, childItem.Item.RawAttributes, attributesInfo);
                            if (bundleItemData != null)
                            {
                                bundleItemDataList.Add(bundleItemData);
                            }
                        }

                        orderItem.SetBundleData(bundleItemDataList);
                    }

                    ctx.Order.OrderItems.Add(orderItem);

                    // Gift cards.
                    if (product.IsGiftCard)
                    {
                        var giftCardInfo = item.AttributeSelection.GetGiftCardInfo();
                        if (giftCardInfo != null)
                        {
                            _db.GiftCards.AddRange(RangeUtility.Create(item.Quantity, () =>  new GiftCard
                            {
                                GiftCardType = product.GiftCardType,
                                PurchasedWithOrderItem = orderItem,
                                Amount = unitPrice.Tax.Value.PriceNet,
                                IsGiftCardActivated = false,
                                GiftCardCouponCode = _giftCardService.GenerateGiftCardCode(),
                                RecipientName = giftCardInfo.RecipientName,
                                RecipientEmail = giftCardInfo.RecipientEmail,
                                SenderName = giftCardInfo.SenderName,
                                SenderEmail = giftCardInfo.SenderEmail,
                                Message = giftCardInfo.Message,
                                IsRecipientNotified = false,
                                CreatedOnUtc = ctx.Now
                            }));
                        }
                    }

                    await _productService.AdjustInventoryAsync(cartItem, true);
                }
            }
            else
            {
                foreach (var oi in ctx.InitialOrder.OrderItems)
                {
                    var newOrderItem = new OrderItem
                    {
                        OrderItemGuid = Guid.NewGuid(),
                        Order = ctx.Order,
                        ProductId = oi.ProductId,
                        Sku = oi.Sku,
                        UnitPriceInclTax = oi.UnitPriceInclTax,
                        UnitPriceExclTax = oi.UnitPriceExclTax,
                        PriceInclTax = oi.PriceInclTax,
                        PriceExclTax = oi.PriceExclTax,
                        TaxRate = oi.TaxRate,
                        AttributeDescription = oi.AttributeDescription,
                        RawAttributes = oi.RawAttributes,
                        Quantity = oi.Quantity,
                        DiscountAmountInclTax = oi.DiscountAmountInclTax,
                        DiscountAmountExclTax = oi.DiscountAmountExclTax,
                        DownloadCount = 0,
                        IsDownloadActivated = false,
                        LicenseDownloadId = 0,
                        ItemWeight = oi.ItemWeight,
                        BundleData = oi.BundleData,
                        ProductCost = oi.ProductCost,
                        DeliveryTimeId = oi.DeliveryTimeId,
                        DisplayDeliveryTime = oi.DisplayDeliveryTime
                    };

                    ctx.Order.OrderItems.Add(newOrderItem);

                    // Gift cards.
                    if (oi.Product.IsGiftCard)
                    {
                        var giftCardInfo = oi.AttributeSelection.GetGiftCardInfo();
                        if (giftCardInfo != null)
                        {
                            for (int i = 0; i < oi.Quantity; i++)
                            {
                                var giftCard = new GiftCard
                                {
                                    GiftCardType = oi.Product.GiftCardType,
                                    PurchasedWithOrderItem = newOrderItem,
                                    Amount = oi.UnitPriceExclTax,
                                    IsGiftCardActivated = false,
                                    GiftCardCouponCode = _giftCardService.GenerateGiftCardCode(),
                                    RecipientName = giftCardInfo.RecipientName,
                                    RecipientEmail = giftCardInfo.RecipientEmail,
                                    SenderName = giftCardInfo.SenderName,
                                    SenderEmail = giftCardInfo.SenderEmail,
                                    Message = giftCardInfo.Message,
                                    IsRecipientNotified = false,
                                    CreatedOnUtc = ctx.Now
                                };

                                _db.GiftCards.Add(giftCard);
                            }
                        }
                    }

                    await _productService.AdjustInventoryAsync(oi, true, oi.Quantity);
                }
            }

            // INFO: CheckOrderStatus performs commit.
        }

        private async Task AddAssociatedData(PlaceOrderContext ctx)
        {
            var order = ctx.Order;

            if (!ctx.PaymentRequest.IsRecurringPayment)
            {
                // Discount usage history.
                foreach (var discount in ctx.AppliedDiscounts)
                {
                    _db.DiscountUsageHistory.Add(new()
                    {
                        DiscountId = discount.Id,
                        OrderId = order.Id,
                        CreatedOnUtc = ctx.Now
                    });
                }

                // Gift card usage history.
                foreach (var giftCard in ctx.CartTotal.AppliedGiftCards)
                {
                    giftCard.GiftCard.GiftCardUsageHistory.Add(new()
                    {
                        GiftCardId = giftCard.GiftCard.Id,
                        UsedWithOrderId = order.Id,
                        UsedValue = giftCard.UsableAmount.Amount,
                        CreatedOnUtc = ctx.Now
                    });
                }

                try
                {
                    // Handle transiancy of uploaded files for checkout attributes.
                    var attributesSelection = ctx.Customer.GenericAttributes.CheckoutAttributes;
                    if (attributesSelection.HasAttributes)
                    {
                        var fileUploadAttributeIds = await _db.CheckoutAttributes
                            .AsQueryable()
                            .Where(x => x.AttributeControlTypeId == (int)AttributeControlType.FileUpload)
                            .Select(x => x.Id)
                            .ToListAsync();

                        if (fileUploadAttributeIds.Count > 0)
                        {
                            var fileGuids = attributesSelection.AttributesMap
                                .Where(x => fileUploadAttributeIds.Contains(x.Key))
                                .SelectMany(x => x.Value)
                                .Select(x => Guid.TryParse(x as string, out Guid guid) ? guid : Guid.Empty)
                                .Where(x => x != Guid.Empty)
                                .ToArray();

                            if (fileGuids.Length > 0)
                            {
                                var downloads = await _db.Downloads
                                    .AsQueryable()
                                    .Where(x => fileGuids.Contains(x.DownloadGuid) && x.IsTransient)
                                    .ToListAsync();

                                downloads.Each(x => x.IsTransient = false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            // Reward points history.
            if (ctx.CartTotal.RedeemedRewardPointsAmount > decimal.Zero)
            {
                var str = _localizationService.GetResource("RewardPoints.Message.RedeemedForOrder", order.CustomerLanguageId);

                ctx.Customer.AddRewardPointsHistoryEntry(
                    -ctx.CartTotal.RedeemedRewardPoints,
                    str.FormatInvariant(order.GetOrderNumber()),
                    order,
                    ctx.CartTotal.RedeemedRewardPointsAmount.Amount);
            }

            // Recurring order.
            if (!ctx.PaymentRequest.IsRecurringPayment && ctx.IsRecurringCart)
            {
                // Create recurring payment (the first payment).
                var rp = new RecurringPayment
                {
                    CycleLength = ctx.PaymentRequest.RecurringCycleLength,
                    CyclePeriod = ctx.PaymentRequest.RecurringCyclePeriod,
                    TotalCycles = ctx.PaymentRequest.RecurringTotalCycles,
                    StartDateUtc = ctx.Now,
                    IsActive = true,
                    CreatedOnUtc = ctx.Now,
                    InitialOrderId = order.Id
                };

                // For RecurringPaymentType.Automatic the history entry will be created later (process is automated).
                if (RecurringPaymentType.Manual == await _paymentService.GetRecurringPaymentTypeAsync(ctx.PaymentRequest.PaymentMethodSystemName))
                {
                    // First payment.
                    rp.RecurringPaymentHistory.Add(new()
                    {
                        CreatedOnUtc = ctx.Now,
                        OrderId = order.Id
                    });
                }

                _db.RecurringPayments.Add(rp);
            }

            // INFO: CheckOrderStatus performs commit.
        }

        private async Task FinalizeOrderPlacement(PlaceOrderContext ctx)
        {
            var order = ctx.Order;
            var notes = new List<string> { T("Admin.OrderNotice.OrderPlaced") };

            // Messages and order notes.
            var msg = await _messageFactory.SendOrderPlacedStoreOwnerNotificationAsync(order, _localizationSettings.DefaultAdminLanguageId);
            if (msg?.Email?.Id != null)
            {
                notes.Add(T("Admin.OrderNotice.MerchantEmailQueued", msg.Email.Id));
            }

            msg = await _messageFactory.SendOrderPlacedCustomerNotificationAsync(order, order.CustomerLanguageId);
            if (msg?.Email?.Id != null)
            {
                notes.Add(T("Admin.OrderNotice.CustomerEmailQueued", msg.Email.Id));
            }

            // Newsletter subscription.
            if (_shoppingCartSettings.NewsletterSubscription != CheckoutNewsletterSubscription.None && ctx.ExtraData.TryGetValue("SubscribeToNewsletter", out var addSubscription))
            {
                var email = ctx.Customer.Email ?? ctx.Customer.Addresses.FirstOrDefault().Email;
                var subscriptionResult = await _newsletterSubscriptionService.ApplySubscriptionAsync(addSubscription.ToBool(), email, order.StoreId);
                if (subscriptionResult.HasValue)
                {
                    notes.Add(T(subscriptionResult.Value ? "Admin.OrderNotice.NewsletterSubscriptionAdded" : "Admin.OrderNotice.NewsletterSubscriptionRemoved"));
                }
            }

            _db.OrderNotes.AddRange(notes.Select(note => new OrderNote
            {
                OrderId = order.Id,
                Note = note,
                CreatedOnUtc = DateTime.UtcNow
            }));

            // Log activity.
            if (!ctx.PaymentRequest.IsRecurringPayment)
            {
                _activityLogger.LogActivity(KnownActivityLogTypes.PublicStorePlaceOrder, T("ActivityLog.PublicStore.PlaceOrder"), order.GetOrderNumber());
            }

            if (!ctx.PaymentRequest.IsRecurringPayment && !ctx.PaymentRequest.IsMultiOrder)
            {
                ctx.Customer.ResetCheckoutData(ctx.PaymentRequest.StoreId, true, true, true, true, true, true);
                await _shoppingCartService.DeleteCartAsync(ctx.Cart, false);
            }

            // INFO: DeleteCartAsync or CheckOrderStatusAsync perform commits.
        }

        class PlaceOrderContext
        {
            public DateTime Now { get; } = DateTime.UtcNow;
            public OrderPlacementResult Result { get; } = new();
            public Order Order { get; } = new();
            public Order InitialOrder { get; init; }
            public Customer Customer { get; init; }
            public Dictionary<string, string> ExtraData { get; init; }
            public ProcessPaymentRequest PaymentRequest { get; init; }
            public ShoppingCart Cart { get; set; }
            public ProductBatchContext BatchContext { get; set; }
            public bool CartRequiresShipping { get; set; }
            public ShoppingCartTotal CartTotal { get; set; }
            public bool IsRecurringCart { get; set; }

            public List<Discount> AppliedDiscounts { get; } = [];

            public void AddDiscount(Discount discount)
            {
                if (discount != null && !AppliedDiscounts.Any(x => x.Id == discount.Id))
                {
                    AppliedDiscounts.Add(discount);
                }
            }
        }

        #endregion
    }
}
