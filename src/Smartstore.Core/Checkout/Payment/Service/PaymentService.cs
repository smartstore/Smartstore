
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment
{
    public partial class PaymentService : AsyncDbSaveHook<PaymentMethod>, IPaymentService
    {
        private const string PAYMENT_METHODS_ALL_KEY = "paymentmethod.all-{0}-";
        private const string PAYMENT_METHODS_PATTERN_KEY = "paymentmethod.*";

        private readonly static object _lock = new();
        private static IList<Type> _paymentMethodFilterTypes = null;

        private readonly SmartDbContext _db;
        private readonly IStoreMappingService _storeMappingService;
        private readonly PaymentSettings _paymentSettings;
        private readonly ICartRuleProvider _cartRuleProvider;
        private readonly IProviderManager _providerManager;
        private readonly IRequestCache _requestCache;
        private readonly ITypeScanner _typeScanner;

        public PaymentService(
            SmartDbContext db,
            IStoreMappingService storeMappingService,
            PaymentSettings paymentSettings,
            ICartRuleProvider cartRuleProvider,
            IProviderManager providerManager,
            IRequestCache requestCache,
            ITypeScanner typeScanner)
        {
            _db = db;
            _storeMappingService = storeMappingService;
            _paymentSettings = paymentSettings;
            _cartRuleProvider = cartRuleProvider;
            _providerManager = providerManager;
            _requestCache = requestCache;
            _typeScanner = typeScanner;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            _requestCache.RemoveByPattern(PAYMENT_METHODS_PATTERN_KEY);

            return Task.CompletedTask;
        }

        #endregion

        public virtual async Task<bool> IsPaymentMethodActiveAsync(string systemName, ShoppingCart cart = null, int storeId = 0)
        {
            Guard.NotEmpty(systemName, nameof(systemName));

            var activePaymentMethods = await LoadActivePaymentMethodsAsync(cart, storeId, null, false);
            var method = activePaymentMethods.FirstOrDefault(x => x.Metadata.SystemName == systemName);

            return method != null;
        }

        public virtual async Task<IEnumerable<Provider<IPaymentMethod>>> LoadActivePaymentMethodsAsync(
            ShoppingCart cart = null,
            int storeId = 0,
            PaymentMethodType[] types = null,
            bool provideFallbackMethod = true)
        {
            var filterRequest = new PaymentFilterRequest
            {
                Cart = cart,
                StoreId = storeId
            };

            var allFilters = GetAllPaymentMethodFilters();
            var allProviders = types != null && types.Any()
                ? (await LoadAllPaymentMethodsAsync(storeId)).Where(x => types.Contains(x.Value.PaymentMethodType))
                : await LoadAllPaymentMethodsAsync(storeId);

            var paymentMethods = await GetAllPaymentMethodsAsync(storeId);

            var activeProviders = await allProviders
                .WhereAwait(async p =>
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
                            await _db.LoadCollectionAsync(pm, x => x.RuleSets);

                            if (!await _cartRuleProvider.RuleMatchesAsync(pm))
                            {
                                return false;
                            }
                        }

                        filterRequest.PaymentMethod = p;

                        // Only payment methods that have not been filtered out.
                        if (await allFilters.AnyAsync(x => x.IsExcludedAsync(filterRequest)))
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
                    fallbackMethod = allProviders.FirstOrDefault(x => x.Metadata?.ModuleDescriptor?.SystemName?.EqualsNoCase("SmartStore.OfflinePayment") ?? false) ?? allProviders.FirstOrDefault();
                }

                if (fallbackMethod != null)
                {
                    return new Provider<IPaymentMethod>[] { fallbackMethod };
                }

                if (DataSettings.DatabaseIsInstalled())
                {
                    throw new InvalidOperationException(T("Payment.OneActiveMethodProviderRequired"));
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

        private async Task<Provider<IPaymentMethod>> LoadMethodOrThrowAsync(string systemName)
        {
            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(systemName);
            return paymentMethod ?? throw new InvalidOperationException(T("Payment.CouldNotLoadMethod"));
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
                    .WhereAwait(async x => !await _storeMappingService.AuthorizeAsync(x, storeId))
                    .Select(x => x.PaymentMethodSystemName)
                    .ToListAsync();

                return providers.Where(x => !unauthorizedMethodNames.Contains(x.Metadata.SystemName));
            }

            return providers;
        }

        public virtual Task<Dictionary<string, PaymentMethod>> GetAllPaymentMethodsAsync(int storeId = 0)
        {
            return _requestCache.GetAsync(PAYMENT_METHODS_ALL_KEY.FormatInvariant(storeId), async () =>
            {
                return await _db.PaymentMethods
                    .AsNoTracking()
                    .Include(x => x.RuleSets)
                    .ApplyStoreFilter(storeId)
                    .ToDictionaryAsync(x => x.PaymentMethodSystemName.EmptyNull(), x => x, StringComparer.OrdinalIgnoreCase);
            });
        }

        public virtual async Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderTotal == decimal.Zero)
                return new();

            var paymentMethod = await LoadMethodOrThrowAsync(processPaymentRequest.PaymentMethodSystemName);
            return await paymentMethod.Value.PreProcessPaymentAsync(processPaymentRequest);
        }

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
            if (processPaymentRequest.CreditCardNumber.HasValue())
            {
                processPaymentRequest.CreditCardNumber = processPaymentRequest.CreditCardNumber.Replace(" ", "");
                processPaymentRequest.CreditCardNumber = processPaymentRequest.CreditCardNumber.Replace("-", "");
            }

            var paymentMethod = await LoadMethodOrThrowAsync(processPaymentRequest.PaymentMethodSystemName);
            return await paymentMethod.Value.ProcessPaymentAsync(processPaymentRequest);
        }

        public virtual async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var order = postProcessPaymentRequest.Order;

            if (order.PaymentMethodSystemName.IsEmpty() || order.OrderTotal == decimal.Zero)
                return;

            var paymentMethod = await LoadMethodOrThrowAsync(order.PaymentMethodSystemName);
            await paymentMethod.Value.PostProcessPaymentAsync(postProcessPaymentRequest);
        }

        public virtual async Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (!_paymentSettings.AllowRePostingPayments)
                return false;

            var paymentMethod = await LoadPaymentMethodBySystemNameAsync(order.PaymentMethodSystemName);
            if (paymentMethod == null)
            {
                // Payment method couldn't be loaded (for example, was uninstalled).
                return false;
            }

            if (paymentMethod.Value.PaymentMethodType is not PaymentMethodType.Redirection and not PaymentMethodType.StandardAndRedirection)
            {
                // This option is available only for redirection payment methods.
                return false;
            }

            if (order.Deleted || order.OrderStatus == OrderStatus.Cancelled || order.PaymentStatus != PaymentStatus.Pending)
            {
                // Do not allow for deleted, cancelled or pending orders.
                return false;
            }

            return await paymentMethod.Value.CanRePostProcessPaymentAsync(order);
        }

        public virtual async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var paymentMethod = await LoadMethodOrThrowAsync(capturePaymentRequest.Order.PaymentMethodSystemName);

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

        public virtual async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var paymentMethod = await LoadMethodOrThrowAsync(refundPaymentRequest.Order.PaymentMethodSystemName);

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

        public virtual async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var paymentMethod = await LoadMethodOrThrowAsync(voidPaymentRequest.Order.PaymentMethodSystemName);

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

        public virtual async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderTotal == decimal.Zero)
            {
                return new()
                {
                    NewPaymentStatus = PaymentStatus.Paid
                };
            }

            var paymentMethod = await LoadMethodOrThrowAsync(processPaymentRequest.PaymentMethodSystemName);

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

        public virtual string GetMaskedCreditCardNumber(string creditCardNumber)
        {
            if (creditCardNumber.IsEmpty())
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

        #region Recurring payment

        public virtual async Task<DateTime?> GetNextRecurringPaymentDateAsync(RecurringPayment recurringPayment)
        {
            Guard.NotNull(recurringPayment, nameof(recurringPayment));

            if (!recurringPayment.IsActive)
            {
                return null;
            }

            await _db.LoadCollectionAsync(recurringPayment, x => x.RecurringPaymentHistory);

            var historyCount = recurringPayment.RecurringPaymentHistory.Count;

            if (historyCount >= recurringPayment.TotalCycles)
            {
                return null;
            }

            DateTime? result = null;
            var cycleLength = recurringPayment.CycleLength;
            var startDate = recurringPayment.StartDateUtc;

            if (historyCount > 0)
            {
                result = recurringPayment.CyclePeriod switch
                {
                    RecurringProductCyclePeriod.Days => startDate.AddDays((double)cycleLength * historyCount),
                    RecurringProductCyclePeriod.Weeks => startDate.AddDays((double)(7 * cycleLength) * historyCount),
                    RecurringProductCyclePeriod.Months => startDate.AddMonths(cycleLength * historyCount),
                    RecurringProductCyclePeriod.Years => startDate.AddYears(cycleLength * historyCount),
                    _ => throw new Exception("Not supported cycle period"),
                };
            }
            else if (recurringPayment.TotalCycles > 0)
            {
                result = recurringPayment.StartDateUtc;
            }

            return result;
        }

        public virtual async Task<int> GetRecurringPaymentRemainingCyclesAsync(RecurringPayment recurringPayment)
        {
            Guard.NotNull(recurringPayment, nameof(recurringPayment));

            await _db.LoadCollectionAsync(recurringPayment, x => x.RecurringPaymentHistory);

            return Math.Clamp(recurringPayment.TotalCycles - recurringPayment.RecurringPaymentHistory.Count, 0, int.MaxValue);
        }

        public virtual async Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            if (cancelPaymentRequest.Order.OrderTotal == decimal.Zero)
                return new CancelRecurringPaymentResult();

            var paymentMethod = await LoadMethodOrThrowAsync(cancelPaymentRequest.Order.PaymentMethodSystemName);

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

        #endregion

        protected virtual IList<IPaymentMethodFilter> GetAllPaymentMethodFilters()
        {
            if (_paymentMethodFilterTypes == null)
            {
                lock (_lock)
                {
                    if (_paymentMethodFilterTypes == null)
                    {
                        _paymentMethodFilterTypes = _typeScanner.FindTypes<IPaymentMethodFilter>().ToList();
                    }
                }
            }

            var paymentMethodFilters = _paymentMethodFilterTypes
                .Select(x => EngineContext.Current.Scope.ResolveUnregistered(x) as IPaymentMethodFilter)
                .ToList();

            return paymentMethodFilters;
        }
    }
}
