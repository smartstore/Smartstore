using Smartstore.Core.Common;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Country entity.
    /// </summary>
    public class CountriesController : WebApiController<Country>
    {
        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public IQueryable<Country> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<Country> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public IQueryable<StateProvince> GetStateProvinces(int key)
        {
            return GetRelatedQuery(key, x => x.StateProvinces);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Create)]
        public async Task<IActionResult> Post([FromBody] Country entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> Put(int key, Delta<Country> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> Patch(int key, Delta<Country> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Country.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
