namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on CollectionGroupMapping entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class CollectionGroupMappingsController : WebApiController<CollectionGroupMapping>
    {
        [HttpGet("CollectionGroupMappings"), ApiQueryable]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public IQueryable<CollectionGroupMapping> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("CollectionGroupMappings({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public SingleResult<CollectionGroupMapping> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("CollectionGroupMappings({key})/CollectionGroup"), ApiQueryable]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public SingleResult<CollectionGroup> GetCollectionGroup(int key)
        {
            return GetRelatedEntity(key, x => x.CollectionGroup);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.CollectionGroup.Update)]
        public Task<IActionResult> Post([FromBody] CollectionGroupMapping model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.CollectionGroup.Update)]
        public Task<IActionResult> Put(int key, Delta<CollectionGroupMapping> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.CollectionGroup.Update)]
        public Task<IActionResult> Patch(int key, Delta<CollectionGroupMapping> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.CollectionGroup.Update)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
