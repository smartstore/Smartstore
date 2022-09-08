using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Seo;

namespace Smartstore.WebApi.Controllers.OData
{
    // Not required anymore (works by convention now):
    // Get<NavigationProperty>: GET /EntitySet(key)/NavigationProperty

    public class CategoriesController : SmartODataController<Category>
    {
        private readonly IUrlService _urlService;

        public CategoriesController(IUrlService urlService)
        {
            _urlService = urlService;
        }

        [Queryable]
        public IActionResult Get(/*ODataQueryOptions<Category> options*/)
        {
            return Ok(Entities.AsNoTracking());
        }

        [Queryable]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
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

        [Queryable]
        [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Patch(int key, Delta<Category> model)
        {
            return await PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();

                var slugResult = await _urlService.ValidateSlugAsync(entity, string.Empty, entity.Name, true);
                await _urlService.ApplySlugAsync(slugResult, true);
            });
        }
    }
}
