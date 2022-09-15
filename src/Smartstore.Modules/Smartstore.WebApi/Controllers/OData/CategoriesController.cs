using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers.OData
{
    // TODO: (mg) (core) try to avoid this insane amount of metadata attributes.
    // Introduce something like a "SmartMetadataCollectorAttribute" or a collecting method in SmartODataController.
    public class CategoriesController : SmartODataController<Category>
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

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        public IActionResult Get()
        {
            return Ok(Entities);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(int key)
        {
            return GetByKeyAsync(key);
        }

        [HttpGet("Categories({key})/{property}")]
        [Permission(Permissions.Catalog.Category.Read)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetProperty(int key, string property)
        {
            return GetPropertyValueAsync(key, property);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.Create)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Category), StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] Category entity)
        {
            return await PostAsync(entity, async () =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Category.Update)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Put(int key, Delta<Category> model)
        {
            return await PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Category.Update)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Patch(int key, Delta<Category> model)
        {
            return await PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Category.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key, async (entity) =>
            {
                await _categoryService.Value.DeleteCategoryAsync(entity);
            });
        }

        private async Task UpdateSlug(Category entity)
        {
            var slugResult = await _urlService.Value.ValidateSlugAsync(entity, string.Empty, entity.Name, true);
            await _urlService.Value.ApplySlugAsync(slugResult, true);
        }
    }
}
