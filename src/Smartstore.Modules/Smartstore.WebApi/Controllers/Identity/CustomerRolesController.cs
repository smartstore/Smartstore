using Microsoft.OData;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on CustomerRole entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Identity)]
    public class CustomerRolesController : WebApiController<CustomerRole>
    {
        [HttpGet("CustomerRoles"), ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public IQueryable<CustomerRole> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("CustomerRoles({key})"), ApiQueryable]
        [Permission(Permissions.Customer.Role.Read)]
        public SingleResult<CustomerRole> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Customer.Role.Create)]
        public Task<IActionResult> Post([FromBody] CustomerRole model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Customer.Role.Update)]
        [ProducesResponseType(Status403Forbidden)]
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
        [ProducesResponseType(Status403Forbidden)]
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
        [ProducesResponseType(Status403Forbidden)]
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
