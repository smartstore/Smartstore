using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductTag entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductTagsController : WebApiController<ProductTag>
    {
        [HttpGet("ProductTags"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductTag> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductTags({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductTag> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public Task<IActionResult> Post([FromBody] ProductTag model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public Task<IActionResult> Put(int key, Delta<ProductTag> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public Task<IActionResult> Patch(int key, Delta<ProductTag> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditTag)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
