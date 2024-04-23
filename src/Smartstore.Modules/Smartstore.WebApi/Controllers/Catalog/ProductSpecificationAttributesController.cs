using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ProductSpecificationAttribute entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductSpecificationAttributesController : WebApiController<ProductSpecificationAttribute>
    {
        [HttpGet("ProductSpecificationAttributes"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductSpecificationAttribute> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ProductSpecificationAttributes({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<ProductSpecificationAttribute> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("ProductSpecificationAttributes({key})/SpecificationAttributeOption"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<SpecificationAttributeOption> GetSpecificationAttributeOption(int key)
        {
            return GetRelatedEntity(key, x => x.SpecificationAttributeOption);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public Task<IActionResult> Post([FromBody] ProductSpecificationAttribute model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public Task<IActionResult> Put(int key, Delta<ProductSpecificationAttribute> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public Task<IActionResult> Patch(int key, Delta<ProductSpecificationAttribute> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditAttribute)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
