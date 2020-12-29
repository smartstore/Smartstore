using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Brands
{
    public class ManufacturerHook : AsyncDbSaveHook<Manufacturer>
    {
        private readonly SmartDbContext _db;

        public ManufacturerHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnInsertedAsync(Manufacturer entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatedAsync(Manufacturer entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update HasDiscountsApplied property.
            var manufacturers = entries
                .Select(x => x.Entity)
                .OfType<Manufacturer>()
                .ToList();

            // TODO: (core) (mg) (PERF) It's not certain that "AppliedDiscounts" has been eager loaded
            manufacturers.Each(x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

            await _db.SaveChangesAsync();
        }
    }
}
