using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Categories
{
    [Important]
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

            foreach (var categoriesChunk in categories.Slice(100))
            {
                var categoryIdsChunk = categoriesChunk
                    .Select(x => x.Id)
                    .ToArray();

                var appliedCategoryIds = await _db.Discounts
                    .SelectMany(x => x.AppliedToCategories)
                    .Where(x => categoryIdsChunk.Contains(x.Id))
                    .Select(x => x.Id)
                    .Distinct()
                    .ToListAsync();

                categoriesChunk.Each(x => x.HasDiscountsApplied = appliedCategoryIds.Contains(x.Id));
            }

            await _db.SaveChangesAsync();
        }
    }
}
