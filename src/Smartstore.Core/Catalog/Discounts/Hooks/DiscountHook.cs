using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Discounts
{
    public class DiscountHook : AsyncDbSaveHook<Discount>
    {
        private readonly SmartDbContext _db;
        private readonly IRequestCache _requestCache;
        private Multimap<string, int> _relatedEntityIds = new(items => new HashSet<int>(items));

        public DiscountHook(SmartDbContext db, IRequestCache requestCache)
        {
            _db = db;
            _requestCache = requestCache;
        }

        protected override Task<HookResult> OnInsertingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.IsPropertyModified(nameof(Discount.DiscountType)))
            {
                KeepRelatedEntityIds(entity);
            }

            return Task.FromResult(HookResult.Ok);
        }

        protected override Task<HookResult> OnDeletingAsync(Discount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            KeepRelatedEntityIds(entity);

            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update HasDiscountsApplied property.
            if (_relatedEntityIds.Any())
            {
                await ProcessChunk(
                    cancelToken,
                    _db.Products,
                    _relatedEntityIds["product"],
                    x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

                await ProcessChunk(
                    cancelToken,
                    _db.Categories,
                    _relatedEntityIds["category"],
                    x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

                await ProcessChunk(
                    cancelToken,
                    _db.Manufacturers,
                    _relatedEntityIds["manufactuter"],
                    x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

                _relatedEntityIds.Clear();
            }

            _requestCache.RemoveByPattern(DiscountService.DISCOUNTS_PATTERN_KEY);
        }

        private void KeepRelatedEntityIds(Discount entity)
        {
            _relatedEntityIds.AddRange("product", entity.AppliedToProducts.Select(x => x.Id));
            _relatedEntityIds.AddRange("category", entity.AppliedToCategories.Select(x => x.Id));
            _relatedEntityIds.AddRange("manufacturer", entity.AppliedToManufacturers.Select(x => x.Id));
        }

        private async Task ProcessChunk<TEntity>(CancellationToken cancelToken, DbSet<TEntity> dbSet, IEnumerable<int> ids, Action<TEntity> process)
            where TEntity : BaseEntity
        {
            var allIds = ids.ToArray();

            foreach (var idsChunk in allIds.Slice(100))
            {
                var entities = await dbSet
                    .Where(x => idsChunk.Contains(x.Id))
                    .ToListAsync(cancelToken);

                foreach (var entity in entities)
                {
                    process(entity);
                }

                await _db.SaveChangesAsync();
            }
        }
    }
}
