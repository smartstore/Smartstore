using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers.V1
{
    /// <summary>
    /// The endpoint for operations on category entity.
    /// </summary>
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

        /// <summary>
        /// Gets a list of all categories.
        /// </summary>
        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public IQueryable<Category> Get()
        {
            return Entities.AsNoTracking();
        }

        /// <summary>
        /// Gets a category by identifier.
        /// </summary>
        /// <param name="key" example="12345">The category identifier.</param>
        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public Task<IActionResult> Get(int key)
        {
            return GetByIdAsync(key);
        }

        /// <summary>
        /// Gets the value of a category property.
        /// </summary>
        /// <remarks>
        /// A property value can alternatively be obtained using the $select query string parameter.
        /// </remarks>
        /// <param name="key" example="12345">The category identifier.</param>
        /// <param name="property" example="Description">The property name.</param>
        [HttpGet("/odata/v1/categories({key})/{property}")]
        [Permission(Permissions.Catalog.Category.Read)]
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
