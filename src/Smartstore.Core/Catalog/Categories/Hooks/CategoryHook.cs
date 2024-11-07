using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Categories
{
    [Important]
    internal class CategoryHook : AsyncDbSaveHook<Category>
    {
        private readonly SmartDbContext _db;
        private readonly IRequestCache _requestCache;

        public CategoryHook(SmartDbContext db, IRequestCache requestCache)
        {
            _db = db;
            _requestCache = requestCache;
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

            foreach (var categoriesChunk in categories.Chunk(100))
            {
                var categoryIdsChunk = categoriesChunk
                    .Select(x => x.Id)
                    .ToArray();

                var appliedCategoryIds = await _db.Discounts
                    .SelectMany(x => x.AppliedToCategories)
                    .Where(x => categoryIdsChunk.Contains(x.Id))
                    .Select(x => x.Id)
                    .Distinct()
                    .ToListAsync(cancelToken);

                categoriesChunk.Each(x => x.HasDiscountsApplied = appliedCategoryIds.Contains(x.Id));
            }

            await _db.SaveChangesAsync(cancelToken);

            // Validate category hierarchy.
            var invalidCategoryIds = new HashSet<int>();
            var modifiedCategories = entries
                .Where(x => x.InitialState == EntityState.Modified)
                .Select(x => x.Entity)
                .OfType<Category>()
                .ToList();

            foreach (var category in modifiedCategories)
            {
                if (!await IsValidCategoryHierarchy(category.Id, category.ParentId, cancelToken))
                {
                    invalidCategoryIds.Add(category.Id);
                }
            }

            if (invalidCategoryIds.Count > 0)
            {
                await _db.Categories
                    .Where(x => invalidCategoryIds.Contains(x.Id))
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.ParentId, p => null), cancelToken);
            }

            _requestCache.RemoveByPattern(CategoryService.CategoriesPatternKey);
        }

        private async Task<bool> IsValidCategoryHierarchy(int categoryId, int? parentCategoryId, CancellationToken cancelToken)
        {
            var parent = await _db.Categories
                .Where(x => x.Id == parentCategoryId)
                .Select(x => new { x.Id, x.ParentId })
                .FirstOrDefaultAsync(cancelToken);

            while (parent?.Id > 0)
            {
                if (categoryId == parent.Id)
                {
                    // Same ID = invalid.
                    return false;
                }

                if (parent.ParentId == null)
                {
                    break;
                }

                parent = await _db.Categories
                    .Where(x => x.Id == parent.ParentId)
                    .Select(x => new { x.Id, x.ParentId })
                    .FirstOrDefaultAsync(cancelToken);
            }

            return true;
        }
    }
}
