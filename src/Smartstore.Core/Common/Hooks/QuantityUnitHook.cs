using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    [Important]
    internal class QuantityUnitHook : AsyncDbSaveHook<QuantityUnit>
    {
        private readonly SmartDbContext _db;

        public QuantityUnitHook(SmartDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default quantity unit.
        /// </summary>
        protected override async Task<HookResult> OnInsertingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await TryResetDefaultQuantityUnitsAsync(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default quantity unit.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await TryResetDefaultQuantityUnitsAsync(entity, cancelToken);

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of quantity unit if it's referenced by products or attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var hasAssociatedEntities = await _db.Products
                .AsNoTracking()
                .Where(x => x.QuantityUnitId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.QuantityUnitId == entity.Id))
                .AnyAsync(cancellationToken: cancelToken);

            if (hasAssociatedEntities)
            {
                throw new SmartException($"The quantity unit '{entity.Name}' cannot be deleted. It has associated products or attribute combinations.");
            }

            return HookResult.Ok;
        }

        public virtual async Task TryResetDefaultQuantityUnitsAsync(QuantityUnit entity, CancellationToken cancelToken)
        {
            Guard.NotNull(entity, nameof(entity));

            if (entity.IsDefault)
            {
                var quantityUnits = await _db.QuantityUnits
                    .Where(x => x.IsDefault && x.Id != entity.Id)
                    .ToListAsync(cancellationToken: cancelToken);

                foreach (var quantityUnit in quantityUnits)
                {
                    quantityUnit.IsDefault = false;
                }
            }
        }
    }
}