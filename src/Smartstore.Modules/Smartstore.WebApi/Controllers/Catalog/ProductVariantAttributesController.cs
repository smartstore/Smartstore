using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductVariantAttribute entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductVariantAttributesController : WebApiController<ProductVariantAttribute>
    {
        [HttpGet("ProductVariantAttributes"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttribute> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductVariantAttributes({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductVariantAttribute> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductVariantAttributes({key})/ProductAttribute"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductAttribute> GetProductAttribute(int key)
        {
            return GetRelatedEntity(key, x => x.ProductAttribute);
        }

        [HttpGet("ProductVariantAttributes({key})/ProductVariantAttributeValues"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttributeValue> GetProductVariantAttributeValues(int key)
        {
            return GetRelatedQuery(key, x => x.ProductVariantAttributeValues);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Post([FromBody] ProductVariantAttribute model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Put(int key, Delta<ProductVariantAttribute> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Patch(int key, Delta<ProductVariantAttribute> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
