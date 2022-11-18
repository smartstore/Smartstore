using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on ProductAttributeOption entity.
    /// </summary>
    public class ProductAttributeOptionsController : WebApiController<ProductAttributeOption>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public IQueryable<ProductAttributeOption> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public SingleResult<ProductAttributeOption> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Post([FromBody] ProductAttributeOption model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Put(int key, Delta<ProductAttributeOption> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Patch(int key, Delta<ProductAttributeOption> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Variant.Read)]
        public SingleResult<ProductAttributeOptionsSet> GetProductAttributeOptionsSet(int key)
        {
            return GetRelatedEntity(key, x => x.ProductAttributeOptionsSet);
        }
    }
}
