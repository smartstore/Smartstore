namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on StateProvince entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class StateProvincesController : WebApiController<StateProvince>
    {
        [HttpGet("StateProvinces"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public IQueryable<StateProvince> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("StateProvinces({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<StateProvince> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("StateProvinces({key})/Country"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<Country> GetCountry(int key)
        {
            return GetRelatedEntity(key, x => x.Country);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Create)]
        public Task<IActionResult> Post([FromBody] StateProvince model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Country.Update)]
        public Task<IActionResult> Put(int key, Delta<StateProvince> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Country.Update)]
        public Task<IActionResult> Patch(int key, Delta<StateProvince> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Country.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
