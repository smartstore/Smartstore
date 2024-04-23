using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on SpecificationAttribute entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class SpecificationAttributesController : WebApiController<SpecificationAttribute>
    {
        [HttpGet("SpecificationAttributes"), ApiQueryable]
        [Permission(Permissions.Catalog.Attribute.Read)]
        public IQueryable<SpecificationAttribute> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("SpecificationAttributes({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Attribute.Read)]
        public SingleResult<SpecificationAttribute> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("SpecificationAttributes({key})/SpecificationAttributeOptions"), ApiQueryable]
        [Permission(Permissions.Catalog.Attribute.Read)]
        public IQueryable<SpecificationAttributeOption> GetSpecificationAttributeOptions(int key)
        {
            return GetRelatedQuery(key, x => x.SpecificationAttributeOptions);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Attribute.Create)]
        public Task<IActionResult> Post([FromBody] SpecificationAttribute model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Attribute.Update)]
        public Task<IActionResult> Put(int key, Delta<SpecificationAttribute> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Attribute.Update)]
        public Task<IActionResult> Patch(int key, Delta<SpecificationAttribute> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Attribute.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
