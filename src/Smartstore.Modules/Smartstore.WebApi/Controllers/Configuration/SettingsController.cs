using Smartstore.Core.Configuration;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Setting entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Platform)]
    public class SettingsController : WebApiController<Setting>
    {
        [HttpGet("Settings"), ApiQueryable]
        [Permission(Permissions.Configuration.Setting.Read)]
        public IQueryable<Setting> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Settings({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Setting.Read)]
        public SingleResult<Setting> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Setting.Create)]
        public Task<IActionResult> Post([FromBody] Setting model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Setting.Update)]
        public Task<IActionResult> Put(int key, Delta<Setting> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Setting.Update)]
        public Task<IActionResult> Patch(int key, Delta<Setting> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Setting.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
