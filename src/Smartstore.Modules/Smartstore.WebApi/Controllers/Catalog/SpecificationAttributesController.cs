using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common.Services;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on SpecificationAttribute entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class SpecificationAttributesController : WebApiController<SpecificationAttribute>
    {
        private readonly Lazy<ICollectionGroupService> _collectionGroupService;

        public SpecificationAttributesController(Lazy<ICollectionGroupService> collectionGroupService)
        {
            _collectionGroupService = collectionGroupService;
        }

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

        #region Actions and functions

        /// <summary>
        /// Applies a collection group name to a specification attribute.
        /// </summary>
        /// <param name="collectionGroupName">
        /// The new collection group name to apply. Adds a CollectionGroup if one does not already exist with this name.
        /// </param>
        [HttpPost("SpecificationAttributes({key})/ApplyCollectionGroupName"), ApiQueryable]
        [Permission(Permissions.Catalog.Attribute.Update)]
        [Produces(Json)]
        [ProducesResponseType(typeof(SpecificationAttribute), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> ApplyCollectionGroupName(int key,
            [FromODataBody] string collectionGroupName)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.CollectionGroupMapping.CollectionGroup));

                await _collectionGroupService.Value.ApplyCollectionGroupNameAsync(entity, collectionGroupName);
                await Db.SaveChangesAsync();

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
