using Microsoft.OData;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on CustomerRole entity.
    /// </summary>
    public class CustomerRolesController : WebApiController<CustomerRole>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public IQueryable<CustomerRole> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public SingleResult<CustomerRole> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Role.Create)]
        public Task<IActionResult> Post([FromBody] CustomerRole entity)
        {
            return PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Customer.Role.Update)]
        public Task<IActionResult> Put(int key, Delta<CustomerRole> model)
        {
            return PutAsync(key, model, async (entity) =>
            {
                CheckCustomerRole(entity);
                await Db.SaveChangesAsync();
            });
        }

        [HttpPatch]
        [Permission(Permissions.Customer.Role.Update)]
        public Task<IActionResult> Patch(int key, Delta<CustomerRole> model)
        {
            return PatchAsync(key, model, async (entity) =>
            {
                CheckCustomerRole(entity);
                await Db.SaveChangesAsync();
            });
        }

        [HttpDelete]
        [Permission(Permissions.Customer.Role.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key, async (entity) =>
            {
                CheckCustomerRole(entity);
                await Db.SaveChangesAsync();
            });
        }

        private static void CheckCustomerRole(CustomerRole entity)
        {
            if (entity != null && entity.IsSystemRole)
            {
                throw new ODataErrorException(new ODataError
                {
                    ErrorCode = Status403Forbidden.ToString(),
                    Message = "Modifying or deleting a system customer role is not allowed."
                });
            }
        }
    }
}
