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
            var query = Entities.AsNoTracking();

            return Ok(query);
        }

        [Queryable]
        public async Task<IActionResult> Get(int key)
        {
            var entity = await Entities.FindByIdAsync(key, false);

            return Ok(entity);
        }

        [HttpGet("Categories({key})/{property}")]
        public Task<IActionResult> GetProperty(int key, string property)
        {
            return GetPropertyValueAsync(key, property);
        }

        [Queryable]
        public async Task<IActionResult> Patch(int key, Delta<Category> model)
        {
            var result = await PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();

                // TODO: "ProcessEntity". Only process slug if "Name" changed.
                var activeSlug = await _urlService.GetActiveSlugAsync(entity.Id, entity.GetEntityName(), 0);
                var validateSlugResult = await _urlService.ValidateSlugAsync(entity, activeSlug, entity.Name, true);
                await _urlService.ApplySlugAsync(validateSlugResult, true);
            });

            return result;
        }
    }
}
