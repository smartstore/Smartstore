using Smartstore.Core.Catalog.Categories;

namespace Smartstore.WebApi.Controllers.OData
{
    public class CategoriesController : ODataControllerBase<Category>
    {
        [HttpGet, Queryable]
        public IActionResult Get(/*ODataQueryOptions<Category> options*/)
        {
            var query = Entities.AsNoTracking();

            return Ok(query);
        }

        [HttpGet, Queryable]
        public async Task<IActionResult> Get(int key)
        {
            var entity = await Entities.FindByIdAsync(key, false);

            return Ok(entity);
        }

        // TODO: (mg) (core) get this shitty OData 8 IODataControllerActionConvention working.
        [HttpGet("odata/v1/Categories({key})/{property}")]
        public Task<IActionResult> GetProperty(int key, string property)
        {
            return GetPropertyValueAsync(key, property);
        }
    }
}
