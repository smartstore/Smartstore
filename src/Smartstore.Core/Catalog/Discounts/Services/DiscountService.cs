using System.Text;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Rules;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Catalog.Discounts
{
    public partial class DiscountService : AsyncDbSaveHook<Discount>, IDiscountService
    {
        // {0} = discountType, {1} = includeHidden, {2} = couponCode.
        private readonly static CompositeFormat DiscountsAllKey = CompositeFormat.Parse("discount.all-{0}-{1}-{2}");
        internal const string DiscountsPatternKey = "discount.*";

        private readonly SmartDbContext _db;
        private readonly IRequestCache _requestCache;
        private readonly IStoreContext _storeContext;
        private readonly ICartRuleProvider _cartRuleProvider;
        private readonly Lazy<IShoppingCartService> _cartService ;
        private readonly Dictionary<DiscountKey, bool> _discountValidityCache = [];
        private readonly Multimap<string, int> _relatedEntityIds = new(items => new HashSet<int>(items));

        public DiscountService(
            SmartDbContext db,
            IRequestCache requestCache,
            IStoreContext storeContext,
            IRuleProviderFactory ruleProviderFactory,
            Lazy<IShoppingCartService> cartService)
        {
            _db = db;
            _requestCache = requestCache;
            _storeContext = storeContext;
            _cartRuleProvider = ruleProviderFactory.GetProvider<ICartRuleProvider>(RuleScope.Cart);
            _cartService = cartService;
        }

        #region Hook

        protected override Task<HookResult> OnInsertedAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override async Task<HookResult> OnUpdatingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.State == EState.Modified)
            {
                var prop = entry.Entry.Property(nameof(entity.DiscountTypeId));

                var currentDiscountType = prop.OriginalValue != null
                    ? (DiscountType)(int)prop.OriginalValue
                    : (DiscountType?)null;

                var newDiscountType = prop.CurrentValue != null
                    ? (DiscountType)(int)prop.CurrentValue
                    : (DiscountType?)null;

                if (currentDiscountType == DiscountType.AssignedToCategories && newDiscountType != DiscountType.AssignedToCategories)
                {
                    await _db.LoadCollectionAsync(entity, x => x.AppliedToCategories, cancelToken: cancelToken);
                    _relatedEntityIds.AddRange("category", entity.AppliedToCategories.Select(x => x.Id));

                    entity.AppliedToCategories.Clear();
                }
                else if (currentDiscountType == DiscountType.AssignedToManufacturers && newDiscountType != DiscountType.AssignedToManufacturers)
                {
                    await _db.LoadCollectionAsync(entity, x => x.AppliedToManufacturers, cancelToken: cancelToken);
                    _relatedEntityIds.AddRange("manufacturer", entity.AppliedToManufacturers.Select(x => x.Id));

                    entity.AppliedToManufacturers.Clear();
                }
                else if (currentDiscountType == DiscountType.AssignedToSkus && newDiscountType != DiscountType.AssignedToSkus)
                {
                    await _db.LoadCollectionAsync(entity, x => x.AppliedToProducts, cancelToken: cancelToken);
                    _relatedEntityIds.AddRange("product", entity.AppliedToProducts.Select(x => x.Id));

                    entity.AppliedToProducts.Clear();
                }
            }

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnUpdatedAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override async Task<HookResult> OnDeletingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.DiscountType == DiscountType.AssignedToCategories)
            {
                await _db.LoadCollectionAsync(entity, x => x.AppliedToCategories, cancelToken: cancelToken);
                _relatedEntityIds.AddRange("category", entity.AppliedToCategories.Select(x => x.Id));
            }
            else if (entity.DiscountType == DiscountType.AssignedToManufacturers)
            {
                await _db.LoadCollectionAsync(entity, x => x.AppliedToManufacturers, cancelToken: cancelToken);
                _relatedEntityIds.AddRange("manufacturer", entity.AppliedToManufacturers.Select(x => x.Id));
            }
            else if (entity.DiscountType == DiscountType.AssignedToSkus)
            {
                await _db.LoadCollectionAsync(entity, x => x.AppliedToProducts, cancelToken: cancelToken);
                _relatedEntityIds.AddRange("product", entity.AppliedToProducts.Select(x => x.Id));
            }

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_relatedEntityIds.Any())
            {
                await UpdateHasDiscountsAppliedProperty(_db.Products, _relatedEntityIds["product"], cancelToken);
                await UpdateHasDiscountsAppliedProperty(_db.Categories, _relatedEntityIds["category"], cancelToken);
                await UpdateHasDiscountsAppliedProperty(_db.Manufacturers, _relatedEntityIds["manufactuter"], cancelToken);

                _relatedEntityIds.Clear();
            }

            _requestCache.RemoveByPattern(DiscountsPatternKey);
        }

        private async Task UpdateHasDiscountsAppliedProperty<TEntity>(DbSet<TEntity> dbSet, IEnumerable<int> ids, CancellationToken cancelToken = default)
            where TEntity : EntityWithDiscounts
        {
            var allIds = ids.ToArray();

            foreach (var idsChunk in allIds.Chunk(100))
            {
                var entities = await dbSet
                    .Include(x => x.AppliedDiscounts)
                    .Where(x => idsChunk.Contains(x.Id))
                    .ToListAsync(cancelToken);

                foreach (var entity in entities.OfType<EntityWithDiscounts>())
                {
                    entity.HasDiscountsApplied = entity.AppliedDiscounts.Any();
                }

                await _db.SaveChangesAsync(cancelToken);
            }
        }

        #endregion

        public virtual async Task<IEnumerable<Discount>> GetAllDiscountsAsync(DiscountType? discountType, string couponCode = null, bool includeHidden = false)
        {
            couponCode = couponCode.EmptyNull();

            var discountTypeId = discountType.HasValue ? (int)discountType.Value : 0;
            var cacheKey = DiscountsAllKey.FormatInvariant(discountTypeId, includeHidden, couponCode);

            return await _requestCache.GetAsync(cacheKey, async () =>
            {
                var query = _db.Discounts.AsQueryable();

                if (discountType.HasValue)
                {
                    switch (discountType.Value)
                    {
                        case DiscountType.AssignedToSkus:
                            query = query.Include(x => x.AppliedToProducts);
                            break;
                        case DiscountType.AssignedToCategories:
                            query = query.Include(x => x.AppliedToCategories);
                            break;
                        case DiscountType.AssignedToManufacturers:
                            query = query.Include(x => x.AppliedToManufacturers);
                            break;
                    }

                    query = query.Where(x => x.DiscountTypeId == discountTypeId);
                }

                if (!includeHidden)
                {
                    var utcNow = DateTime.UtcNow;

                    query = query.Where(x =>
                        (!x.StartDateUtc.HasValue || x.StartDateUtc <= utcNow) &&
                        (!x.EndDateUtc.HasValue || x.EndDateUtc >= utcNow));
                }

                if (couponCode.HasValue())
                {
                    query = query.Where(x => x.CouponCode == couponCode);
                }

                var discounts = await query
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                return discounts;
            });
        }

        public virtual async Task<bool> IsDiscountValidAsync(
            Discount discount,
            Customer customer,
            string couponCodeToValidate,
            Store store = null,
            DiscountValidationFlags flags = DiscountValidationFlags.All)
        {
            Guard.NotNull(discount);

            store ??= _storeContext.CurrentStore;

            var cacheKey = new DiscountKey(discount, customer, couponCodeToValidate, store, flags);
            if (_discountValidityCache.TryGetValue(cacheKey, out var result))
            {
                return result;
            }

            // Check coupon code.
            if (discount.RequiresCouponCode && (discount.CouponCode.IsEmpty() || !discount.CouponCode.Trim().EqualsNoCase(couponCodeToValidate)))
            {
                return Cached(false);
            }

            // Check date range.
            if (!discount.IsDateInRange())
            {
                return Cached(false);
            }

            if (flags.HasFlag(DiscountValidationFlags.DiscountLimitations) && !await CheckDiscountLimitationsAsync(discount, customer))
            {
                return Cached(false);
            }

            ShoppingCart cart = null;

            // Do not to apply discounts if there are gift cards in the cart cause the customer could "earn" money through that.
            if (flags.HasFlag(DiscountValidationFlags.GiftCards) &&
                (discount.DiscountType == DiscountType.AssignedToOrderTotal || discount.DiscountType == DiscountType.AssignedToOrderSubTotal))
            {
                cart = await _cartService.Value.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (cart.Items.Any(x => x.Item?.Product != null && x.Item.Product.IsGiftCard))
                {
                    return Cached(false);
                }
            }

            // Rules.
            if (flags.HasFlag(DiscountValidationFlags.CartRules))
            {
                await _db.LoadCollectionAsync(discount, x => x.RuleSets);

                var contextAction = (CartRuleContext context) =>
                {
                    context.Customer = customer;
                    context.Store = store;
                    context.ShoppingCart = cart;
                };

                if (!await _cartRuleProvider.RuleMatchesAsync(discount))
                {
                    return Cached(false);
                }
            }

            return Cached(true);

            bool Cached(bool value)
            {
                _discountValidityCache[cacheKey] = value;
                return value;
            }
        }

        public virtual async Task<bool> ApplyDiscountsAsync<T>(T entity, int[] selectedDiscountIds, DiscountType type)
            where T : BaseEntity, IDiscountable
        {
            Guard.NotNull(entity);

            selectedDiscountIds ??= Array.Empty<int>();

            await _db.LoadCollectionAsync(entity, x => x.AppliedDiscounts);

            if (!selectedDiscountIds.Any() && !entity.AppliedDiscounts.Any())
            {
                // Nothing to do.
                return false;
            }

            var updated = false;
            var discounts = (await GetAllDiscountsAsync(type, null, true)).ToDictionary(x => x.Id);

            foreach (var discountId in discounts.Keys)
            {
                if (selectedDiscountIds.Contains(discountId))
                {
                    if (!entity.AppliedDiscounts.Any(x => x.Id == discountId))
                    {
                        entity.AppliedDiscounts.Add(discounts[discountId]);
                        updated = true;
                    }
                }
                else if (entity.AppliedDiscounts.Any(x => x.Id == discountId))
                {
                    entity.AppliedDiscounts.Remove(discounts[discountId]);
                    updated = true;
                }
            }

            return updated;
        }

        protected virtual async Task<bool> CheckDiscountLimitationsAsync(Discount discount, Customer customer)
        {
            Guard.NotNull(discount);

            switch (discount.DiscountLimitation)
            {
                case DiscountLimitationType.NTimesOnly:
                {
                    var count = await _db.DiscountUsageHistory
                        .ApplyStandardFilter(discount.Id)
                        .CountAsync();
                    return count < discount.LimitationTimes;
                }

                case DiscountLimitationType.NTimesPerCustomer:
                    if (customer != null && !customer.IsGuest())
                    {
                        // Registered customer.
                        var count = await _db.DiscountUsageHistory
                            .Include(x => x.Order)
                            .ApplyStandardFilter(discount.Id, customer.Id)
                            .CountAsync();
                        return count < discount.LimitationTimes;
                    }
                    else
                    {
                        // Guest.
                        return true;
                    }

                case DiscountLimitationType.Unlimited:
                    return true;

                default:
                    return false;
            }
        }

        class DiscountKey : Tuple<Discount, Customer, string, Store, DiscountValidationFlags>
        {
            public DiscountKey(Discount discount, Customer customer, string customerCouponCode, Store store, DiscountValidationFlags flags)
                : base(discount, customer, customerCouponCode, store, flags)
            {
            }
        }
    }
}
