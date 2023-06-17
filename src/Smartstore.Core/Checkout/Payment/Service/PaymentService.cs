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
        // 0 = withRules
        private const string PAYMENT_METHODS_ALL_KEY = "paymentmethod.all-{0}";
        private const string PAYMENT_METHODS_PATTERN_KEY = "paymentmethod.*";
        private const string PAYMENT_METHOD_FILTERS_ALL_KEY = "paymentmethodfilters.all";

        private readonly static object _lock = new();
        private static IList<Type> _paymentMethodFilterTypes = null;

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly PaymentSettings _paymentSettings;
        private readonly ICartRuleProvider _cartRuleProvider;
        private readonly IProviderManager _providerManager;
        private readonly IRequestCache _requestCache;
        private readonly ITypeScanner _typeScanner;
        private readonly IModuleConstraint _moduleConstraint;

        // All providers request cache. Dictionary key = SystemName.
        private readonly Lazy<Dictionary<string, Provider<IPaymentMethod>>> _providersCache;

        // Provider enabled states request cache. Key: (SystemName, StoreId)
        private readonly Dictionary<object, bool> _enabledStates = new();

        // Provider active states request cache. Key: (SystemName, StoreId, ShoppingCart)
        private readonly Dictionary<object, bool> _activeStates = new();

        public PaymentService(
            SmartDbContext db,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            PaymentSettings paymentSettings,
            ICartRuleProvider cartRuleProvider,
            IProviderManager providerManager,
            IRequestCache requestCache,
            ITypeScanner typeScanner,
            IModuleConstraint moduleConstraint)
        {
            _db = db;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _paymentSettings = paymentSettings;
            _cartRuleProvider = cartRuleProvider;
            _providerManager = providerManager;
            _requestCache = requestCache;
            _typeScanner = typeScanner;
            _moduleConstraint = moduleConstraint;

            _providersCache = new Lazy<Dictionary<string, Provider<IPaymentMethod>>>(() => 
            {
                return _providerManager.GetAllProviders<IPaymentMethod>()
                    .ToDictionarySafe(x => x.Metadata.SystemName, StringComparer.OrdinalIgnoreCase);
            }, false);
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

        #region Provider management

        public virtual Task<bool> IsPaymentProviderEnabledAsync(string systemName, int storeId = 0)
        {
            Guard.NotEmpty(systemName);

            var provider = _providersCache.Value.Get(systemName);
            if (provider == null)
            {
                return Task.FromResult(false);
            }

            return GetEnabledStateAsync(provider, storeId);
        }

        public virtual async Task<bool> IsPaymentProviderActiveAsync(string systemName, ShoppingCart cart = null, int storeId = 0)
        {
            Guard.NotEmpty(systemName);

            var provider = _providersCache.Value.Get(systemName);
            if (provider == null)
            {
                return false;
            }

            return 
                await GetEnabledStateAsync(provider, storeId) && 
                await GetActiveStateAsync(provider, storeId, cart, null);
        }

        private async Task<bool> GetEnabledStateAsync(Provider<IPaymentMethod> provider, int storeId)
        {
            var sysName = provider.Metadata.SystemName;
            if (!_enabledStates.TryGetValue((sysName, storeId), out var enabled))
            {
                if (storeId == 0)
                {
                    enabled = provider.IsPaymentProviderEnabled(_paymentSettings);
                    _enabledStates[(sysName, 0)] = enabled;
                }
                else
                {
                    // If store-less entry is disabled, the store-specific entry is also disabled.
                    enabled = await GetEnabledStateAsync(provider, 0);
                    if (enabled)
                    {
                        // If store-less entry is enabled, the store-specific entry must still be checked.
                        // First check if container module is enabled.
                        enabled = _moduleConstraint.Matches(provider.Metadata.ModuleDescriptor, storeId);

                        if (enabled && !QuerySettings.IgnoreMultiStore)
                        {
                            // Then check if payment method entity (if any) is limited to this store.
                            var allMethods = await GetAllPaymentMethodsAsync(false);
                            var method = allMethods.Get(sysName);
                            enabled = method == null || await _storeMappingService.AuthorizeAsync(method, storeId);
                        }
                    }

                    _enabledStates[(sysName, storeId)] = enabled;
                }
            }

            return enabled;
        }

        private async Task<bool> GetActiveStateAsync(
            Provider<IPaymentMethod> provider,
            int storeId,
            ShoppingCart cart = null,
            Dictionary<string, PaymentMethod> allMethods = null,
            IList<IPaymentMethodFilter> allFilters = null)
        {
            var sysName = provider.Metadata.SystemName;
            var cacheKey = (sysName, storeId, cart);
            if (_activeStates.TryGetValue(cacheKey, out var active)) 
            {
                return active;
            }

            try
            {
                // We gonna need expanded entities for rule matching.
                allMethods ??= await GetAllPaymentMethodsAsync(true);

                // Rule matching
                if (allMethods.TryGetValue(sysName, out var method))
                {
                    var contextAction = (CartRuleContext context) =>
                    {
                        context.ShoppingCart = cart;
                        if (storeId > 0 && storeId != context.Store.Id)
                        {
                            context.Store = _storeContext.GetStoreById(storeId);
                        }
                    };

                    if (!await _cartRuleProvider.RuleMatchesAsync(method, contextAction: contextAction))
                    {
                        return Cached(false);
                    }
                }

                allFilters ??= GetAllPaymentMethodFilters();
                if (allFilters.Count > 0)
                {
                    var filterRequest = new PaymentFilterRequest
                    {
                        Cart = cart,
                        StoreId = storeId,
                        PaymentProvider = provider
                    };

                    // Only payment providers that have not been filtered out.
                    if (await allFilters.AnyAsync(x => x.IsExcludedAsync(filterRequest)))
                    {
                        return Cached(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return Cached(true);

            bool Cached(bool value)
            {
                // Put state to request cache
                _activeStates[cacheKey] = value;
                return value;
            }
        }

        public virtual async Task<IEnumerable<Provider<IPaymentMethod>>> LoadActivePaymentProvidersAsync(
            ShoppingCart cart = null,
            int storeId = 0,
            PaymentMethodType[] types = null,
            bool provideFallbackMethod = true)
        {
            var allPaymentMethods = await GetAllPaymentMethodsAsync(true);
            var allFilters = GetAllPaymentMethodFilters();

            var allProviders = await LoadAllPaymentProvidersAsync(true, storeId);
            if (!types.IsNullOrEmpty())
            {
                allProviders = allProviders.Where(x => types.Contains(x.Value.PaymentMethodType));
            }

            var activeProviders = await allProviders
                .WhereAwait(x => GetActiveStateAsync(x, storeId, cart, allPaymentMethods, allFilters))
                .ToListAsync();

            if (!activeProviders.Any() && provideFallbackMethod)
            {
                var fallbackMethod = allProviders.FirstOrDefault(x => x.IsPaymentProviderEnabled(_paymentSettings))
                    ?? allProviders.FirstOrDefault(x => x.Metadata?.ModuleDescriptor?.SystemName?.EqualsNoCase("Smartstore.OfflinePayment") ?? false)
                    ?? allProviders.FirstOrDefault();

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

        public virtual async Task<Provider<IPaymentMethod>> LoadPaymentProviderBySystemNameAsync(string systemName, bool onlyWhenEnabled = false, int storeId = 0)
        {
            var provider = _providersCache.Value.Get(systemName);
            var checkEnabled = onlyWhenEnabled || storeId > 0;

            if (provider == null || checkEnabled && !await GetEnabledStateAsync(provider, storeId))
            {
                return null;
            }

            return provider;
        }

        public virtual async Task<IEnumerable<Provider<IPaymentMethod>>> LoadAllPaymentProvidersAsync(bool onlyEnabled = false, int storeId = 0)
        {
            var providers = _providersCache.Value.Values.AsEnumerable();

            if (onlyEnabled || storeId > 0)
            {
                providers = await providers
                    .WhereAwait(x => GetEnabledStateAsync(x, storeId))
                    .ToListAsync();
            }

            return providers;
        }

        public virtual async Task<Dictionary<string, PaymentMethod>> GetAllPaymentMethodsAsync(bool withRules = false)
        {
            var withRulesCacheKey = PAYMENT_METHODS_ALL_KEY.FormatInvariant(true);
            if (_requestCache.Contains(withRulesCacheKey))
            {
                // Always return upgraded entities if they are cached.
                return _requestCache.Get<Dictionary<string, PaymentMethod>>(withRulesCacheKey);
            }

            var noRulescacheKey = PAYMENT_METHODS_ALL_KEY.FormatInvariant(false);
            if (!withRules)
            {
                if (_requestCache.Contains(noRulescacheKey))
                {
                    // Return unexpanded entities from cache when they are requested
                    return _requestCache.Get<Dictionary<string, PaymentMethod>>(noRulescacheKey);
                }
            }
            
            if (withRules)
            {
                // Try to remove unexpanded entities when an "upgrade" is requested
                _requestCache.Remove(noRulescacheKey);
            }

            var query = _db.PaymentMethods.AsNoTracking();

            if (withRules)
            {
                query = query
                    .AsSplitQuery()
                    .Include(x => x.RuleSets)
                    .ThenInclude(x => x.Rules);
            }

            var result = await query.ToDictionaryAsync(x => x.PaymentMethodSystemName.EmptyNull(), x => x, StringComparer.OrdinalIgnoreCase);

            // Put result to request cache.
            _requestCache.Put(withRules ? withRulesCacheKey : noRulescacheKey, result);

            return result;
        }

        protected virtual IList<IPaymentMethodFilter> GetAllPaymentMethodFilters()
        {
            if (_paymentMethodFilterTypes == null)
            {
                lock (_lock)
                {
                    _paymentMethodFilterTypes ??= _typeScanner.FindTypes<IPaymentMethodFilter>().ToList();
                }
            }

            var paymentMethodFilters = _requestCache.Get(PAYMENT_METHOD_FILTERS_ALL_KEY, () =>
            {
                return _paymentMethodFilterTypes
                    .Select(x => EngineContext.Current.Scope.ResolveUnregistered(x) as IPaymentMethodFilter)
                    .ToList();
            });

            return paymentMethodFilters;
        }

        #endregion

        #region Payment processing

        public virtual async Task<PreProcessPaymentResult> PreProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderTotal == decimal.Zero)
            {
                return new();
            }     

            var provider = await LoadProviderOrThrowAsync(processPaymentRequest.PaymentMethodSystemName);
            return await provider.Value.PreProcessPaymentAsync(processPaymentRequest);
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

            var provider = await LoadProviderOrThrowAsync(processPaymentRequest.PaymentMethodSystemName);
            return await provider.Value.ProcessPaymentAsync(processPaymentRequest);
        }

        public virtual async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var order = postProcessPaymentRequest.Order;

            if (order.PaymentMethodSystemName.IsEmpty() || order.OrderTotal == decimal.Zero)
            {
                return;
            } 

            var paymentMethod = await LoadProviderOrThrowAsync(order.PaymentMethodSystemName);
            await paymentMethod.Value.PostProcessPaymentAsync(postProcessPaymentRequest);
        }

        public virtual async Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            Guard.NotNull(order);

            if (!_paymentSettings.AllowRePostingPayments)
            {
                return false;
            }    

            var provider = await LoadPaymentProviderBySystemNameAsync(order.PaymentMethodSystemName);
            if (provider == null)
            {
                // Payment method couldn't be loaded (for example, was uninstalled).
                return false;
            }

            if (provider.Value.PaymentMethodType is not PaymentMethodType.Redirection and not PaymentMethodType.StandardAndRedirection)
            {
                // This option is available only for redirection payment methods.
                return false;
            }

            if (order.Deleted || order.OrderStatus == OrderStatus.Cancelled || order.PaymentStatus != PaymentStatus.Pending)
            {
                // Do not allow for deleted, cancelled or pending orders.
                return false;
            }

            return await provider.Value.CanRePostProcessPaymentAsync(order);
        }

        public virtual async Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            var provider = await LoadProviderOrThrowAsync(capturePaymentRequest.Order.PaymentMethodSystemName);

            try
            {
                return await provider.Value.CaptureAsync(capturePaymentRequest);
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
            var provider = await LoadProviderOrThrowAsync(refundPaymentRequest.Order.PaymentMethodSystemName);

            try
            {
                return await provider.Value.RefundAsync(refundPaymentRequest);
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
            var provider = await LoadProviderOrThrowAsync(voidPaymentRequest.Order.PaymentMethodSystemName);

            try
            {
                return await provider.Value.VoidAsync(voidPaymentRequest);
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

            var paymentMethod = await LoadProviderOrThrowAsync(processPaymentRequest.PaymentMethodSystemName);

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
            {
                return string.Empty;
            }
                
            if (creditCardNumber.Length <= 4)
            {
                return creditCardNumber;
            }

            var last4 = creditCardNumber.Substring(creditCardNumber.Length - 4, 4);
            var maskedChars = string.Empty;
            for (var i = 0; i < creditCardNumber.Length - 4; i++)
            {
                maskedChars += "*";
            }
            return maskedChars + last4;
        }

        private async Task<Provider<IPaymentMethod>> LoadProviderOrThrowAsync(string systemName)
        {
            var provider = await LoadPaymentProviderBySystemNameAsync(systemName);
            return provider ?? throw new InvalidOperationException(T("Payment.CouldNotLoadMethod"));
        }

        #endregion

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
            {
                return new CancelRecurringPaymentResult();
            }   

            var provider = await LoadProviderOrThrowAsync(cancelPaymentRequest.Order.PaymentMethodSystemName);

            try
            {
                return await provider.Value.CancelRecurringPaymentAsync(cancelPaymentRequest);
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
    }
}
