using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Category entity.
    /// </summary>
    public class CategoriesController : WebApiController<Category>
    {
        private readonly Lazy<IUrlService> _urlService;
        private readonly Lazy<ICategoryService> _categoryService;

        public CategoriesController(
            Lazy<IUrlService> urlService,
            Lazy<ICategoryService> categoryService)
        {
            _urlService = urlService;
            _categoryService = categoryService;
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public IQueryable<Category> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public SingleResult<Category> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public IQueryable<Discount> GetAppliedDiscounts(int key)
        {
            return GetRelatedQuery(key, x => x.AppliedDiscounts);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.Create)]
        public Task<IActionResult> Post([FromBody] Category model)
        {
            return PostAsync(model, async () =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(model);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Category.Update)]
        public Task<IActionResult> Put(int key, Delta<Category> model)
        {
            return PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Category.Update)]
        public Task<IActionResult> Patch(int key, Delta<Category> model)
        {
            return PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Category.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key, async (entity) =>
            {
                await _categoryService.Value.DeleteCategoryAsync(entity);
            });
        }

        private async Task UpdateSlug(Category entity)
        {
            var slugResult = await _urlService.Value.ValidateSlugAsync(entity, string.Empty, true);
            await _urlService.Value.ApplySlugAsync(slugResult, true);
        }
    }
}
