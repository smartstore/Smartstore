namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Address entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class AddressesController : WebApiController<Address>
    {
        [HttpGet("Addresses"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public IQueryable<Address> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Addresses({key})"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Address> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("Addresses({key})/Country"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<Country> GetCountry(int key)
        {
            return GetRelatedEntity(key, x => x.Country);
        }

        [HttpGet("Addresses({key})/StateProvince"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<StateProvince> GetStateProvince(int key)
        {
            return GetRelatedEntity(key, x => x.StateProvince);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Create)]
        public Task<IActionResult> Post([FromBody] Address model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Customer.Update)]
        public Task<IActionResult> Put(int key, Delta<Address> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Customer.Update)]
        public Task<IActionResult> Patch(int key, Delta<Address> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Customer.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
