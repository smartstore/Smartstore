using Smartstore.Core.DataExchange;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on SyncMapping entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Platform)]
    public class SyncMappingsController : WebApiController<SyncMapping>
    {
        [HttpGet("SyncMappings"), ApiQueryable]
        public IQueryable<SyncMapping> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("SyncMappings({key})"), ApiQueryable]
        public SingleResult<SyncMapping> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        public Task<IActionResult> Post([FromBody] SyncMapping model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        public Task<IActionResult> Put(int key, Delta<SyncMapping> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        public Task<IActionResult> Patch(int key, Delta<SyncMapping> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
