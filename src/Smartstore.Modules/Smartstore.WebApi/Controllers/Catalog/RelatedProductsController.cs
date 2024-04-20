using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on RelatedProduct entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class RelatedProductsController : WebApiController<RelatedProduct>
    {
        [HttpGet("RelatedProducts"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<RelatedProduct> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("RelatedProducts({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<RelatedProduct> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public Task<IActionResult> Post([FromBody] RelatedProduct model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public Task<IActionResult> Put(int key, Delta<RelatedProduct> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public Task<IActionResult> Patch(int key, Delta<RelatedProduct> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditPromotion)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
