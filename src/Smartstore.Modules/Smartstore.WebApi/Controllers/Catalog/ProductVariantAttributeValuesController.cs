using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductVariantAttributeValue entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductVariantAttributeValuesController : WebApiController<ProductVariantAttributeValue>
    {
        [HttpGet("ProductVariantAttributeValues"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttributeValue> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductVariantAttributeValues({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductVariantAttributeValue> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductVariantAttributeValues({key})/ProductVariantAttribute"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductVariantAttribute> GetProductVariantAttribute(int key)
        {
            return GetRelatedEntity(key, x => x.ProductVariantAttribute);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Post([FromBody] ProductVariantAttributeValue model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Put(int key, Delta<ProductVariantAttributeValue> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        public Task<IActionResult> Patch(int key, Delta<ProductVariantAttributeValue> model)
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
