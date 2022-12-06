using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on SpecificationAttributeOption entity.
    /// </summary>
    public class SpecificationAttributeOptionsController : WebApiController<SpecificationAttributeOption>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Attribute.Read)]
        public IQueryable<SpecificationAttributeOption> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Attribute.Read)]
        public SingleResult<SpecificationAttributeOption> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Attribute.Read)]
        public SingleResult<SpecificationAttribute> GetSpecificationAttribute(int key)
        {
            return GetRelatedEntity(key, x => x.SpecificationAttribute);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductSpecificationAttribute> GetProductSpecificationAttributes(int key)
        {
            return GetRelatedQuery(key, x => x.ProductSpecificationAttributes);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public Task<IActionResult> Post([FromBody] SpecificationAttributeOption model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public Task<IActionResult> Put(int key, Delta<SpecificationAttributeOption> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public Task<IActionResult> Patch(int key, Delta<SpecificationAttributeOption> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
