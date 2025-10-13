namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on CollectionGroup entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class CollectionGroupsController : WebApiController<CollectionGroup>
    {
        [HttpGet("CollectionGroups"), ApiQueryable]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public IQueryable<CollectionGroup> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("CollectionGroups({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public SingleResult<CollectionGroup> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("CollectionGroups({key})/CollectionGroupMappings"), ApiQueryable]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public IQueryable<CollectionGroupMapping> GetCollectionGroupMappings(int key)
        {
            return GetRelatedQuery(key, x => x.CollectionGroupMappings);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.CollectionGroup.Create)]
        public Task<IActionResult> Post([FromBody] CollectionGroup model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.CollectionGroup.Update)]
        public Task<IActionResult> Put(int key, Delta<CollectionGroup> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.CollectionGroup.Update)]
        public Task<IActionResult> Patch(int key, Delta<CollectionGroup> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.CollectionGroup.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
