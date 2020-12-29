using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Categories
{
    public class CategoryHook : AsyncDbSaveHook<Category>
    {
        private readonly SmartDbContext _db;

        public CategoryHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnInsertedAsync(Category entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatedAsync(Category entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Update HasDiscountsApplied property.
            var categories = entries
                .Select(x => x.Entity)
                .OfType<Category>()
                .ToList();

            // TODO: (core) (mg) (PERF) It's not certain that "AppliedDiscounts" has been eager loaded
            categories.Each(x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

            await _db.SaveChangesAsync();
        }
    }
}
