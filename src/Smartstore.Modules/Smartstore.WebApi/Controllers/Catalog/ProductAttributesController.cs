using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductAttribute entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductAttributesController : WebApiController<ProductAttribute>
    {
        [HttpGet("ProductAttributes"), ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public IQueryable<ProductAttribute> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductAttributes({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public SingleResult<ProductAttribute> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductAttributes({key})/ProductAttributeOptionsSets"), ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public IQueryable<ProductAttributeOptionsSet> GetProductAttributeOptionsSets(int key)
        {
            return GetRelatedQuery(key, x => x.ProductAttributeOptionsSets);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.Create)]
        public Task<IActionResult> Post([FromBody] ProductAttribute model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Variant.Update)]
        public Task<IActionResult> Put(int key, Delta<ProductAttribute> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Variant.Update)]
        public Task<IActionResult> Patch(int key, Delta<ProductAttribute> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Variant.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
