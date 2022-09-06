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

        [HttpGet]
        public Task<IActionResult> GetProperty(int key, string propertyName)
        {
            return GetPropertyValueAsync(key, propertyName);
        }
    }
}
