using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductAttributeOptionsSet entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductAttributeOptionsSetsController : WebApiController<ProductAttributeOptionsSet>
    {
        [HttpGet("ProductAttributeOptionsSets"), ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public IQueryable<ProductAttributeOptionsSet> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductAttributeOptionsSets({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public SingleResult<ProductAttributeOptionsSet> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductAttributeOptionsSets({key})/ProductAttribute"), ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public SingleResult<ProductAttribute> GetProductAttribute(int key)
        {
            return GetRelatedEntity(key, x => x.ProductAttribute);
        }

        [HttpGet("ProductAttributeOptionsSets({key})/ProductAttributeOptions"), ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public IQueryable<ProductAttributeOption> GetProductAttributeOptions(int key)
        {
            return GetRelatedQuery(key, x => x.ProductAttributeOptions);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Post([FromBody] ProductAttributeOptionsSet model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Put(int key, Delta<ProductAttributeOptionsSet> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Patch(int key, Delta<ProductAttributeOptionsSet> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
