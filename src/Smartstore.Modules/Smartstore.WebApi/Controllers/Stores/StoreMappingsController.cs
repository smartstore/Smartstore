using Smartstore.Core.Stores;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on StoreMapping entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Platform)]
    public class StoreMappingsController : WebApiController<StoreMapping>
    {
        [HttpGet("StoreMappings"), ApiQueryable]
        public IQueryable<StoreMapping> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("StoreMappings({key})"), ApiQueryable]
        public SingleResult<StoreMapping> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        public Task<IActionResult> Post([FromBody] StoreMapping model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        public Task<IActionResult> Put(int key, Delta<StoreMapping> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        public Task<IActionResult> Patch(int key, Delta<StoreMapping> model)
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
