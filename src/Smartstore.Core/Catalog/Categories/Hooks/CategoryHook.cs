using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Categories
{
    [Important]
    public class CategoryHook : AsyncDbSaveHook<Category>
    {
        private readonly SmartDbContext _db;
        private readonly ICategoryService _categoryService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryHook(
            SmartDbContext db,
            ICategoryService categoryService,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _categoryService = categoryService;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HookResult> OnInsertedAsync(Category entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnUpdatedAsync(Category entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Soft-delete sub-categories.
            var softDeletedCategories = entries
                .Where(x => x.IsSoftDeleted == true)
                .Select(x => x.Entity)
                .OfType<Category>()
                .ToList();

            var subCategoryIds = await GetSubCategoryIds(softDeletedCategories.Select(x => x.Id));
            await SoftDeleteCategories(subCategoryIds, SoftDeleteChildCategories);


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

            // Validate category hierarchy.
            var invalidCategoryIds = new HashSet<int>();
            var modifiedCategories = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Modified)
                .Select(x => x.Entity)
                .OfType<Category>()
                .ToList();

            foreach (var category in modifiedCategories)
            {
                var valid = await IsValidCategoryHierarchy(category.Id, category.ParentCategoryId);
                if (!valid)
                {
                    invalidCategoryIds.Add(category.Id);
                }
            }

            if (invalidCategoryIds.Any())
            {
                var num = await _db.Categories
                    .Where(x => invalidCategoryIds.Contains(x.Id))
                    .BatchUpdateAsync(x => new Category { ParentCategoryId = 0 });
            }
        }

        private async Task<IEnumerable<int>> GetSubCategoryIds(IEnumerable<int> categoryIds)
        {
            var result = new HashSet<int>();
            var ids = categoryIds.Distinct().ToArray();

            foreach (var id in ids)
            {
                var tree = await _categoryService.GetCategoryTreeAsync(id, true);
                if (tree?.HasChildren ?? false)
                {
                    result.AddRange(tree.Children.Select(x => x.Value.Id));
                }
            }

            return result;
        }

        private async Task SoftDeleteCategories(IEnumerable<int> categoryIds, bool softDelete)
        {
            if (!categoryIds.Any())
            {
                return;
            }

            var num = await _db.Categories
                .Where(x => categoryIds.Contains(x.Id))
                .BatchUpdateAsync(x => new Category 
                {
                    Deleted = softDelete || x.Deleted,
                    ParentCategoryId = softDelete ? x.ParentCategoryId : 0
                });

            // Process sub-categories.
            var subCategoryIds = await GetSubCategoryIds(categoryIds);
            await SoftDeleteCategories(subCategoryIds, softDelete);
        }

        private async Task<bool> IsValidCategoryHierarchy(int categoryId, int parentCategoryId)
        {
            var parent = await _db.Categories
                .Where(x => x.Id == parentCategoryId)
                .Select(x => new { x.Id, x.ParentCategoryId })
                .FirstOrDefaultAsync();

            while (parent?.Id > 0)
            {
                if (categoryId == parent.Id)
                {
                    // Same ID > invalid.
                    return false;
                }

                if (parent.ParentCategoryId == 0)
                {
                    break;
                }

                parent = await _db.Categories
                    .Where(x => x.Id == parent.ParentCategoryId)
                    .Select(x => new { x.Id, x.ParentCategoryId })
                    .FirstOrDefaultAsync();
            }

            return true;
        }

        private bool SoftDeleteChildCategories
        {
            get
            {
                var values = StringValues.Empty;

                // TODO: (mg) (web) Use "ChildCategoriesDeleteType" (no longer "deleteType") as unique form key for deleting child categories.
                if (_httpContextAccessor.HttpContext?.Request?.Form?.TryGetValue("ChildCategoriesDeleteType", out values) ?? false)
                {
                    return values.FirstOrDefault().EqualsNoCase("deletechilds");
                }

                return false;
            }
        }
    }
}
