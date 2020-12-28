using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Discounts
{
    public class DiscountHook : AsyncDbSaveHook<Discount>
    {
        private readonly SmartDbContext _db;
        private Multimap<string, int> _relatedEntityIds = new Multimap<string, int>();

        public DiscountHook(SmartDbContext db)
        {
            _db = db;
        }

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
                await ChunkAsync(
                    _db.Products,
                    _relatedEntityIds["product"],
                    x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

                await ChunkAsync(
                    _db.Categories,
                    _relatedEntityIds["category"],
                    x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

                await ChunkAsync(
                    _db.Manufacturers,
                    _relatedEntityIds["manufactuter"],
                    x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

                _relatedEntityIds.Clear();
            }
        }

        private void KeepRelatedEntityIds(Discount entity)
        {
            _relatedEntityIds.AddRange("product", entity.AppliedToProducts.Select(x => x.Id));
            _relatedEntityIds.AddRange("category", entity.AppliedToCategories.Select(x => x.Id));
            _relatedEntityIds.AddRange("manufacturer", entity.AppliedToManufacturers.Select(x => x.Id));
        }

        private async Task ChunkAsync<TEntity>(DbSet<TEntity> dbSet, IEnumerable<int> ids, Action<TEntity> process)
            where TEntity : BaseEntity
        {
            var allIds = ids.Distinct().ToArray();

            foreach (var idsChunk in allIds.Slice(100))
            {
                var entities = await dbSet
                    .Where(x => idsChunk.Contains(x.Id))
                    .ToListAsync();

                foreach (var entity in entities)
                {
                    process(entity);
                }

                await _db.SaveChangesAsync();
            }
        }
    }
}
