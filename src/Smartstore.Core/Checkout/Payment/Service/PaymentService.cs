using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Engine.Modularity;
using StackExchange.Profiling.Internal;

namespace Smartstore.Core.Checkout.Payment.Service
{
    public partial class PaymentService //: IPaymentService
    {
        private const string PAYMENT_METHODS_ALL_KEY = "SmartStore.paymentmethod.all-{0}-";

        private readonly static object _lock = new();
        private static IList<Type> _paymentMethodFilterTypes = null;

        private readonly SmartDbContext _db;
        private readonly IStoreMappingService _storeMappingService;
        private readonly PaymentSettings _paymentSettings;
        private readonly ICartRuleProvider _cartRuleProvider;
        private readonly IProviderManager _providerManager;
        private readonly ICommonServices _services;
        //private readonly ITypeFinder _typeFinder;

        public PaymentService(
            SmartDbContext db,
            IStoreMappingService storeMappingService,
            PaymentSettings paymentSettings,
            ICartRuleProvider cartRuleProvider,
            IProviderManager providerManager,
            ICommonServices services
            //ITypeFinder typeFinder
            )
        {
            _db = db;
            _storeMappingService = storeMappingService;
            _paymentSettings = paymentSettings;
            _cartRuleProvider = cartRuleProvider;
            _providerManager = providerManager;
            _services = services;
            //_typeFinder = typeFinder;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        #region Methods

        public virtual async Task<bool> IsPaymentMethodActiveAsync(string systemName, int storeId = 0)
        {
            var method = await LoadPaymentMethodBySystemNameAsync(systemName, true, storeId);
            return method != null;
        }

        public virtual async Task<bool> IsPaymentMethodActiveAsync(
            string systemName,
            Customer customer = null,
            IList<OrganizedShoppingCartItem> cart = null,
            int storeId = 0)
        {
            Guard.NotEmpty(systemName, nameof(systemName));

            var activePaymentMethods = await LoadActivePaymentMethodsAsync(customer, cart, storeId, null, false);
            var method = activePaymentMethods.FirstOrDefault(x => x.Metadata.SystemName == systemName);

            return method != null;
        }

        public virtual async Task<IEnumerable<Provider<IPaymentMethod>>> LoadActivePaymentMethodsAsync(
            Customer customer = null,
            IList<OrganizedShoppingCartItem> cart = null,
            int storeId = 0,
            PaymentMethodType[] types = null,
            bool provideFallbackMethod = true)
        {
            var filterRequest = new PaymentFilterRequest
            {
                Cart = cart,
                StoreId = storeId,
                Customer = customer
            };

            var allFilters = GetAllPaymentMethodFilters();
            var allProviders = types != null && types.Any()
                ? (await LoadAllPaymentMethodsAsync(storeId)).Where(x => types.Contains(x.Value.PaymentMethodType))
                : await LoadAllPaymentMethodsAsync(storeId);

            var paymentMethods = await GetAllPaymentMethodsAsync(storeId);

            var activeProviders = await allProviders
                .WhereAsync(async p =>
                {
                    try
                    {
                        // Only active payment methods.
                        if (!p.Value.IsActive || !_paymentSettings.ActivePaymentMethodSystemNames.Contains(p.Metadata.SystemName, StringComparer.InvariantCultureIgnoreCase))
                        {
                            return false;
                        }

                        // Rule sets.
                        if (paymentMethods.TryGetValue(p.Metadata.SystemName, out var pm))
                        {
                            if (!(await _cartRuleProvider.RuleMatchesAsync(pm)))
                            {
                                return false;
                            }
                        }

                        filterRequest.PaymentMethod = p;

                        // Only payment methods that have not been filtered out.
                        if (await allFilters.AnyAsync(async x => { return await x.IsExcludedAsync(filterRequest); }))
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    return true;
                })
                .ToListAsync();

            if (!activeProviders.Any() && provideFallbackMethod)
            {
                var fallbackMethod = allProviders.FirstOrDefault(x => x.IsPaymentMethodActive(_paymentSettings));
                if (fallbackMethod == null)
                {
                    // TODO: Plugin descriptor is missing

                    //fallbackMethod = allProviders.FirstOrDefault(x => x.Metadata?.PluginDescriptor?.SystemName?.IsCaseInsensitiveEqual("SmartStore.OfflinePayment") ?? false)
                    //    ?? allProviders.FirstOrDefault();
                }

                if (fallbackMethod != null)
                {
                    return new Provider<IPaymentMethod>[] { fallbackMethod };
                }

                if (DataSettings.DatabaseIsInstalled())
                {
                    throw new SmartException(T("Payment.OneActiveMethodProviderRequired"));
                }
            }

            return activeProviders;
        }

        public virtual async Task<Provider<IPaymentMethod>> LoadPaymentMethodBySystemNameAsync(string systemName, bool onlyWhenActive = false, int storeId = 0)
        {
            var provider = _providerManager.GetProvider<IPaymentMethod>(systemName, storeId);
            if (provider == null || onlyWhenActive && !provider.IsPaymentMethodActive(_paymentSettings))
            {
                return null;
            }

            if (!QuerySettings.IgnoreMultiStore && storeId > 0)
            {
                // Return provider if paymentMethod is null
                var paymentMethod = _db.PaymentMethods.FirstOrDefault(x => x.PaymentMethodSystemName == systemName);
                if (paymentMethod != null && !await _storeMappingService.AuthorizeAsync(paymentMethod, storeId))
                {
                    return null;
                }
            }

            return provider;
        }

        public virtual async Task<IEnumerable<Provider<IPaymentMethod>>> LoadAllPaymentMethodsAsync(int storeId = 0)
        {
            var providers = _providerManager.GetAllProviders<IPaymentMethod>(storeId);
            if (providers.Any() && !QuerySettings.IgnoreMultiStore && storeId > 0)
            {
                var unauthorizedMethods = await _db.PaymentMethods
                    .AsNoTracking()
                    .Where(x => x.LimitedToStores)
                    .ToListAsync();

                var unauthorizedMethodNames = await unauthorizedMethods
                    .WhereAsync(async x => !await _storeMappingService.AuthorizeAsync(x, storeId))
                    .Select(x => x.PaymentMethodSystemName)
                    .ToListAsync();

                return providers.Where(x => !unauthorizedMethodNames.Contains(x.Metadata.SystemName));
            }

            return providers;
        }

        public virtual Task<Dictionary<string, PaymentMethod>> GetAllPaymentMethodsAsync(int storeId = 0)
        {
            return _services.RequestCache.GetAsync(PAYMENT_METHODS_ALL_KEY.FormatInvariant(storeId), async () =>
            {
                return await _db.PaymentMethods
                    .AsNoTracking()
                    .ApplyStoreFilter(storeId)
                    .ToDictionaryAsync(x => x.PaymentMethodSystemName.EmptyNull(), x => x, StringComparer.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// Pre process a payment.
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing.</param>
        /// <returns>Pre process payment result.</returns>
        public virtual async Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderTotal == decimal.Zero)
                return new();

            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(processPaymentRequest.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            return await paymentMethod.Value.PreProcessPaymentAsync(processPaymentRequest);
        }

        /// <summary>
        /// Process a payment.
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing.</param>
        /// <returns>Process payment result.</returns>
        public virtual async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderTotal == decimal.Zero)
            {
                return new()
                {
                    NewPaymentStatus = PaymentStatus.Paid
                };
            }

            // Remove any white space or dashes from credit card number
            if (!processPaymentRequest.CreditCardNumber.IsNullOrWhiteSpace())
            {
                processPaymentRequest.CreditCardNumber = processPaymentRequest.CreditCardNumber.Replace(" ", "");
                processPaymentRequest.CreditCardNumber = processPaymentRequest.CreditCardNumber.Replace("-", "");
            }

            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(processPaymentRequest.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            return await paymentMethod.Value.ProcessPaymentAsync(processPaymentRequest);
        }

        /// <summary>
        /// Post process payment (e.g. used by payment gateways to redirect to a third-party URL).
        /// Called after an order has been placed or when customer re-post the payment.
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing.</param>
        public virtual async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (!postProcessPaymentRequest.Order.PaymentMethodSystemName.HasValue())
                return;

            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(postProcessPaymentRequest.Order.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            await paymentMethod.Value.PostProcessPaymentAsync(postProcessPaymentRequest);
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods).
        /// </summary>
        /// <param name="order">Order.</param>
        /// <returns><c>True</c> if order can re post process payment, otherwise <c>false</c></returns>
        public virtual async Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (!_paymentSettings.AllowRePostingPayments)
                return false;

            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(order.PaymentMethodSystemName);
            if (paymentMethod == null)
            {
                // Payment method couldn't be loaded (for example, was uninstalled)
                return false;
            }

            if (paymentMethod.Value.PaymentMethodType is not PaymentMethodType.Redirection and not PaymentMethodType.StandardAndRedirection)
            {
                // This option is available only for redirection payment methods
                return false;
            }

            if (order.Deleted || order.OrderStatus == OrderStatus.Cancelled || order.PaymentStatus != PaymentStatus.Pending)
            {
                // Do not allow for deleted, cancelled or pending orders
                return false;
            }

            return await paymentMethod.Value.CanRePostProcessPaymentAsync(order);
        }

        /// <summary>
        /// Gets an additional handling fee of a payment method.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>Additional handling fee</returns>
        public virtual async Task<Money> GetAdditionalHandlingFeeAsync(IList<OrganizedShoppingCartItem> cart, string paymentMethodSystemName)
        {
            var currency = _services.WorkContext.WorkingCurrency;
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            var paymentMethodAdditionalFee = paymentMethod != null
                ? await paymentMethod.Value.GetAdditionalHandlingFeeAsync(cart)
                : decimal.Zero;

            return currency.AsMoney(paymentMethodAdditionalFee);
        }

        /// <summary>
        /// Gets a value indicating whether the payment method supports capture.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether capture is supported.</returns>
        public virtual async Task<bool> SupportCaptureAsync(string paymentMethodSystemName)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;

            return paymentMethod.Value.SupportCapture;
        }

        /// <summary>
        /// Captures payment.
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request.</param>
        /// <returns>Capture payment result.</returns>
        public virtual async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(capturePaymentRequest.Order.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            try
            {
                return await paymentMethod.Value.CaptureAsync(capturePaymentRequest);
            }
            catch (NotSupportedException)
            {
                var result = new CapturePaymentResult();
                result.Errors.Add(T("Common.Payment.NoCaptureSupport"));
                return result;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported by payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether partial refund is supported.</returns>
        public virtual async Task<bool> SupportPartiallyRefundAsync(string paymentMethodSystemName)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;

            return paymentMethod.Value.SupportPartiallyRefund;
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported by payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether refund is supported.</returns>
        public virtual async Task<bool> SupportRefundAsync(string paymentMethodSystemName)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;

            return paymentMethod.Value.SupportRefund;
        }

        /// <summary>
        /// Refunds a payment.
        /// </summary>
        /// <param name="refundPaymentRequest">Refund payment request.</param>
        /// <returns>Refund payment result.</returns>
        public virtual async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(refundPaymentRequest.Order.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            try
            {
                return await paymentMethod.Value.RefundAsync(refundPaymentRequest);
            }
            catch (NotSupportedException)
            {
                var result = new RefundPaymentResult();
                result.Errors.Add(T("Common.Payment.NoRefundSupport"));
                return result;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported by payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A value indicating whether void is supported.</returns>
        public virtual async Task<bool> SupportVoidAsync(string paymentMethodSystemName)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
                return false;

            return paymentMethod.Value.SupportVoid;
        }

        /// <summary>
        /// Voids a payment.
        /// </summary>
        /// <param name="voidPaymentRequest">Void payment request.</param>
        /// <returns>Void payment result.</returns>
        public virtual async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(voidPaymentRequest.Order.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            try
            {
                return await paymentMethod.Value.VoidAsync(voidPaymentRequest);
            }
            catch (NotSupportedException)
            {
                var result = new VoidPaymentResult();
                result.Errors.Add(T("Common.Payment.NoVoidSupport"));
                return result;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A recurring payment type of payment method.</returns>
        public virtual async Task<RecurringPaymentType> GetRecurringPaymentTypeAsync(string paymentMethodSystemName)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
                return RecurringPaymentType.NotSupported;

            return paymentMethod.Value.RecurringPaymentType;
        }

        /// <summary>
        /// Process recurring payment.
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing.</param>
        /// <returns>Process payment result.</returns>
        public virtual async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderTotal == decimal.Zero)
            {
                return new()
                {
                    NewPaymentStatus = PaymentStatus.Paid
                };
            }

            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(processPaymentRequest.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            try
            {
                return await paymentMethod.Value.ProcessRecurringPaymentAsync(processPaymentRequest);
            }
            catch (NotSupportedException)
            {
                var result = new ProcessPaymentResult();
                result.Errors.Add(T("Common.Payment.NoRecurringPaymentSupport"));
                return result;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Cancels a recurring payment.
        /// </summary>
        /// <param name="cancelPaymentRequest">Cancel recurring payment request.</param>
        /// <returns>Cancel recurring payment result.</returns>
        public virtual async Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            if (cancelPaymentRequest.Order.OrderTotal == decimal.Zero)
                return new CancelRecurringPaymentResult();

            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(cancelPaymentRequest.Order.PaymentMethodSystemName);
            if (paymentMethod == null)
                throw new SmartException(T("Payment.CouldNotLoadMethod"));

            try
            {
                return await paymentMethod.Value.CancelRecurringPaymentAsync(cancelPaymentRequest);
            }
            catch (NotSupportedException)
            {
                var result = new CancelRecurringPaymentResult();
                result.Errors.Add(T("Common.Payment.NoRecurringPaymentSupport"));
                return result;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a payment method type.
        /// </summary>
        /// <param name="paymentMethodSystemName">Payment method system name.</param>
        /// <returns>A payment method type.</returns>
        public virtual async Task<PaymentMethodType> GetPaymentMethodTypeAsync(string paymentMethodSystemName)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(paymentMethodSystemName);
            if (paymentMethod == null)
                return PaymentMethodType.Unknown;

            return paymentMethod.Value.PaymentMethodType;
        }

        /// <summary>
        /// Gets masked credit card number.
        /// </summary>
        /// <param name="creditCardNumber">Credit card number.</param>
        /// <returns>Masked credit card number.</returns>
        public virtual string GetMaskedCreditCardNumber(string creditCardNumber)
        {
            if (creditCardNumber.IsNullOrWhiteSpace())
                return string.Empty;

            if (creditCardNumber.Length <= 4)
                return creditCardNumber;

            var last4 = creditCardNumber.Substring(creditCardNumber.Length - 4, 4);
            var maskedChars = string.Empty;
            for (var i = 0; i < creditCardNumber.Length - 4; i++)
            {
                maskedChars += "*";
            }
            return maskedChars + last4;
        }

        public virtual IList<IPaymentMethodFilter> GetAllPaymentMethodFilters()
        {
            if (_paymentMethodFilterTypes == null)
            {
                lock (_lock)
                {
                    if (_paymentMethodFilterTypes == null)
                    {
                        //_paymentMethodFilterTypes = _typeFinder.FindClassesOfType<IPaymentMethodFilter>(ignoreInactivePlugins: true).ToList();
                    }
                }
            }

            // TODO: (ms) (core) ContainerManager is missing

            var paymentMethodFilters = _paymentMethodFilterTypes
                //.Select(x => EngineContext.Current.ContainerManager.ResolveUnregistered(x) as IPaymentMethodFilter)
                .Select(x => x as IPaymentMethodFilter)
                .ToList();

            return paymentMethodFilters;
        }
        #endregion
    }
}
