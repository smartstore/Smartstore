using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on CustomerRoleMapping entity.
    /// </summary>
    public class CustomerRoleMappingsController : WebApiController<CustomerRoleMapping>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public IQueryable<CustomerRoleMapping> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public SingleResult<CustomerRoleMapping> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Customer> GetCustomer(int key)
        {
            return GetRelatedEntity(key, x => x.Customer);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public SingleResult<CustomerRole> GetCustomerRole(int key)
        {
            return GetRelatedEntity(key, x => x.CustomerRole);
        }

        [HttpPost]
        [Permission(Permissions.Customer.EditRole)]
        public async Task<IActionResult> Post([FromBody] CustomerRoleMapping entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Customer.EditRole)]
        public async Task<IActionResult> Put(int key, Delta<CustomerRoleMapping> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Customer.EditRole)]
        public async Task<IActionResult> Patch(int key, Delta<CustomerRoleMapping> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Customer.EditRole)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
