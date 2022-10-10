using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Brands
{
    [Important]
    internal class ManufacturerHook : AsyncDbSaveHook<Manufacturer>
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

            foreach (var manufacturersChunk in manufacturers.Chunk(100))
            {
                var manufacturerIdsChunk = manufacturersChunk
                    .Select(x => x.Id)
                    .ToArray();

                var appliedManufacturerIds = await _db.Discounts
                    .SelectMany(x => x.AppliedToManufacturers)
                    .Where(x => manufacturerIdsChunk.Contains(x.Id))
                    .Select(x => x.Id)
                    .Distinct()
                    .ToListAsync(cancelToken);

                manufacturersChunk.Each(x => x.HasDiscountsApplied = appliedManufacturerIds.Contains(x.Id));
            }

            await _db.SaveChangesAsync(cancelToken);
        }
    }
}
