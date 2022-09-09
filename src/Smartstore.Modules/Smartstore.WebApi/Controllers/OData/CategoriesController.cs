using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Seo;

namespace Smartstore.WebApi.Controllers.OData
{
    // TODO: (mg) (core) add permission check and authentication.
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

        [HttpGet, Queryable]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        public IActionResult Get()
        {
            return Ok(Entities);
        }

        [HttpGet, Queryable]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> Get(int key)
        {
            return GetByKeyAsync(key);
        }

        [HttpGet("Categories({key})/{property}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetProperty(int key, string property)
        {
            return GetPropertyValueAsync(key, property);
        }

        [HttpPost]
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
