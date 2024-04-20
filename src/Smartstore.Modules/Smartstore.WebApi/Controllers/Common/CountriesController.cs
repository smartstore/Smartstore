namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Country entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class CountriesController : WebApiController<Country>
    {
        [HttpGet("Countries"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public IQueryable<Country> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Countries({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<Country> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("Countries({key})/StateProvinces"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public IQueryable<StateProvince> GetStateProvinces(int key)
        {
            return GetRelatedQuery(key, x => x.StateProvinces);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Create)]
        public Task<IActionResult> Post([FromBody] Country model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Country.Update)]
        public Task<IActionResult> Put(int key, Delta<Country> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Country.Update)]
        public Task<IActionResult> Patch(int key, Delta<Country> model)
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
