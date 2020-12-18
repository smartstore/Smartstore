using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Core.Common.Hooks
{
    public class QuantityUnitHook : AsyncDbSaveHook<QuantityUnit>
    {
        private readonly SmartDbContext _db;

        public QuantityUnitHook(SmartDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Sets all quantity units to <see cref="QuantityUnit.IsDefault"/> = false if the currently updated entity is the default delivery time.
        /// </summary>
        protected override async Task<HookResult> OnUpdatingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.IsDefault)
            {
                var temp = new List<QuantityUnit> { entity };
                var qus = await _db.QuantityUnits
                    .Where(x => x.IsDefault && x.Id != entity.Id)
                    .ToListAsync();

                foreach (var qu in qus)
                {
                    qu.IsDefault = false;
                }
            }

            return HookResult.Ok;
        }

        /// <summary>
        /// Prevents saving of quantity unit if it's referenced in products or attribute combinations.
        /// </summary>
        protected override async Task<HookResult> OnDeletingAsync(QuantityUnit entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var query =
                from x in _db.Products
                where x.QuantityUnitId == entity.Id || x.ProductVariantAttributeCombinations.Any(c => c.QuantityUnitId == entity.Id)
                select x.Id;

            if (await query.AnyAsync(cancellationToken: cancelToken))
            {
                throw new SmartException("The quantity unit cannot be deleted. It has associated product variants.");
            }

            return HookResult.Ok;
        }
    }
}