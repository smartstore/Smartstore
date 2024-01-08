using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;

namespace Smartstore.Core.Checkout.Payment
{
    public partial class PaymentService : AsyncDbSaveHook<BaseEntity>, IPaymentService
    {
        // 0 = withRules
        const string PaymentMethodsAllKey = "payment:method:all:{0}";
        const string PaymentMethodsPatternKey = "payment:method:*";
        const string PaymentMethodsFiltersAllKey = "payment:methodfilters:all";

        // 0 = SystemName, 1 = StoreId
        const string PaymentProviderEnabledKey = "payment:provider:enabled:{0}-{1}";
        const string PaymentProviderEnabledPatternKey = "payment:provider:enabled:*";

        // 0 = StoreId
        public const string ProductDetailPaymentIcons = "productdetail:paymenticons:{0}";
        public const string ProductDetailPaymentIconsPatternKey = "productdetail:paymenticons:*";

        private readonly static object _lock = new();
        private static IList<Type> _paymentMethodFilterTypes = null;

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly IStoreMappingService _storeMappingService;
        private readonly PaymentSettings _paymentSettings;
        private readonly ICartRuleProvider _cartRuleProvider;
        private readonly IProviderManager _providerManager;
        private readonly ICacheManager _cache;
        private readonly IRequestCache _requestCache;
        private readonly ITypeScanner _typeScanner;
        private readonly IModuleConstraint _moduleConstraint;

        // All providers request cache. Dictionary key = SystemName.
        private readonly Lazy<Dictionary<string, Provider<IPaymentMethod>>> _providersCache;

        // Provider active states request cache. Key: (SystemName, StoreId, ShoppingCart)
        private readonly Dictionary<object, bool> _activeStates = new();

        public PaymentService(
            SmartDbContext db,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            PaymentSettings paymentSettings,
            IRuleProviderFactory ruleProviderFactory,
            IProviderManager providerManager,
            ICacheManager cache,
            IRequestCache requestCache,
            ITypeScanner typeScanner,
            IModuleConstraint moduleConstraint)
        {
            _db = db;
            _storeContext = storeContext;
            _storeMappingService = storeMappingService;
            _paymentSettings = paymentSettings;
            _cartRuleProvider = ruleProviderFactory.GetProvider<ICartRuleProvider>(RuleScope.Cart);
            _providerManager = providerManager;
            _cache = cache;
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

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity;
            if (entity is Setting setting)
            {
                var invalidate = 
                    (setting.Name.StartsWithNoCase("PluginSetting.") && setting.Name.EndsWithNoCase(".LimitedToStores")) ||
                    setting.Name.EqualsNoCase(TypeHelper.NameOf<PaymentSettings>(x => x.ActivePaymentMethodSystemNames, true));
                if (invalidate)
                {
                    await _cache.RemoveByPatternAsync(PaymentProviderEnabledPatternKey);
                }
            }
            else if (entity is PaymentMethod)
            {
                await _cache.RemoveByPatternAsync(PaymentProviderEnabledPatternKey);
                _requestCache.RemoveByPattern(PaymentMethodsPatternKey);
            }
            else if (entity is StoreMapping storeMapping)
            {
                if (NamedEntity.GetEntityName<PaymentMethod>() == storeMapping.EntityName)
                {
                    await _cache.RemoveByPatternAsync(PaymentProviderEnabledPatternKey);
                }
            }
            else
            {
                return HookResult.Void;
            }

            return HookResult.Ok;
        }

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            _requestCache.RemoveByPattern(PaymentMethodsPatternKey);

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

        private Task<bool> GetEnabledStateAsync(Provider<IPaymentMethod> provider, int storeId)
        {
            var sysName = provider.Metadata.SystemName;
            var cacheKey = PaymentProviderEnabledKey.FormatInvariant(sysName, storeId);

            return _cache.GetAsync(cacheKey, async () => 
            {
                var enabled = provider.IsPaymentProviderEnabled(_paymentSettings);

                if (storeId > 0 && enabled)
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

                return enabled;
            });
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
                    throw new PaymentException(T("Payment.OneActiveMethodProviderRequired"));
                }
            }

            return activeProviders;
        }

        public virtual async Task<Provider<IPaymentMethod>> LoadPaymentProviderBySystemNameAsync(string systemName, bool onlyWhenEnabled = false, int storeId = 0)
        {
            if (systemName.IsEmpty())
            {
                return null;
            }

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
            var withRulesCacheKey = PaymentMethodsAllKey.FormatInvariant(true);
            if (_requestCache.Contains(withRulesCacheKey))
            {
                // Always return upgraded entities if they are cached.
                return _requestCache.Get<Dictionary<string, PaymentMethod>>(withRulesCacheKey);
            }

            var noRulescacheKey = PaymentMethodsAllKey.FormatInvariant(false);
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

            var paymentMethodFilters = _requestCache.Get(PaymentMethodsFiltersAllKey, () =>
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

            var provider = await LoadProviderOrThrow(processPaymentRequest.PaymentMethodSystemName);
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
                processPaymentRequest.CreditCardNumber = processPaymentRequest.CreditCardNumber.Replace(" ", string.Empty);
                processPaymentRequest.CreditCardNumber = processPaymentRequest.CreditCardNumber.Replace("-", string.Empty);
            }

            var provider = await LoadProviderOrThrow(processPaymentRequest.PaymentMethodSystemName);
            return await provider.Value.ProcessPaymentAsync(processPaymentRequest);
        }

        public virtual async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var order = postProcessPaymentRequest.Order;

            if (order.PaymentMethodSystemName.IsEmpty() || order.OrderTotal == decimal.Zero)
            {
                return;
            } 

            var paymentMethod = await LoadProviderOrThrow(order.PaymentMethodSystemName);
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
            var provider = await LoadProviderOrThrow(capturePaymentRequest.Order.PaymentMethodSystemName);
            return await provider.Value.CaptureAsync(capturePaymentRequest);
        }

        public virtual async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            var provider = await LoadProviderOrThrow(refundPaymentRequest.Order.PaymentMethodSystemName);
            return await provider.Value.RefundAsync(refundPaymentRequest);
        }

        public virtual async Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            var provider = await LoadProviderOrThrow(voidPaymentRequest.Order.PaymentMethodSystemName);
            return await provider.Value.VoidAsync(voidPaymentRequest);
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

        private async Task<Provider<IPaymentMethod>> LoadProviderOrThrow(string systemName)
        {
            var provider = await LoadPaymentProviderBySystemNameAsync(systemName);
            return provider ?? throw new PaymentException(T("Payment.CouldNotLoadMethod"));
        }

        #endregion

        #region Recurring payment

        public virtual async Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            if (processPaymentRequest.OrderTotal == decimal.Zero)
            {
                return new()
                {
                    NewPaymentStatus = PaymentStatus.Paid
                };
            }

            var provider = await LoadProviderOrThrow(processPaymentRequest.PaymentMethodSystemName);
            return await provider.Value.ProcessRecurringPaymentAsync(processPaymentRequest);
        }

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
                    _ => throw new PaymentException("Not supported cycle period"),
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

            var provider = await LoadProviderOrThrow(cancelPaymentRequest.Order.PaymentMethodSystemName);
            return await provider.Value.CancelRecurringPaymentAsync(cancelPaymentRequest);
        }

        #endregion
    }
}
