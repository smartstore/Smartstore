using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Services
{
    [Important]
    public partial class QuantityUnitService : AsyncDbSaveHook<QuantityUnit>, IQuantityUnitService
    {
        private readonly SmartDbContext _db;
        private readonly CatalogSettings _catalogSettings;

        public QuantityUnitService(SmartDbContext db, CatalogSettings catalogSettings)
        {
            _db = db;
            _catalogSettings = catalogSettings;
        }

        #region Hook

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default quantity unit.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.IsDefault)
            {
                var quantityUnits = await _db.QuantityUnits
                    .AsQueryable()
                    .Where(x => x.IsDefault && x.Id != entity.Id)
                    .ToListAsync(cancellationToken: cancelToken);

                foreach (var quantityUnit in quantityUnits)
                {
                    quantityUnit.IsDefault = false;
                }
            }

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of quantity unit if it's referenced in products or attribute combinations.
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

        #endregion

        public virtual async Task<QuantityUnit> GetQuantityUnitByIdAsync(int? quantityUnitId, bool tracked = false)
        {
            var id = quantityUnitId ?? 0;
            if (id == 0)
            {
                if (_catalogSettings.ShowDefaultQuantityUnit)
                {
                    return await _db.QuantityUnits
                        .ApplyTracking(tracked)
                        .FirstOrDefaultAsync(x => x.IsDefault);
                }
                else
                {
                    return null;
                }
            }

            return await _db.QuantityUnits.FindByIdAsync(id, tracked);
        }
    }
}
