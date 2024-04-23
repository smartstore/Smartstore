using Smartstore.Core.Stores;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Address entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Platform)]
    public class StoresController : WebApiController<Store>
    {
        [HttpGet("Stores"), ApiQueryable]
        [Permission(Permissions.Configuration.Store.Read)]
        public IQueryable<Store> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Stores({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Store.Read)]
        public SingleResult<Store> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Store.Create)]
        public Task<IActionResult> Post([FromBody] Store model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Store.Update)]
        public Task<IActionResult> Put(int key, Delta<Store> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Store.Update)]
        public Task<IActionResult> Patch(int key, Delta<Store> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Store.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
