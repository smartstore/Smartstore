using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on RewardPointsHistory entity.
    /// </summary>
    public class RewardPointsHistoryController : WebApiController<RewardPointsHistory>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public IQueryable<RewardPointsHistory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<RewardPointsHistory> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Order> GetUsedWithOrder(int key)
        {
            return GetRelatedEntity(key, x => x.UsedWithOrder);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Customer> GetCustomer(int key)
        {
            return GetRelatedEntity(key, x => x.Customer);
        }

        [HttpPost, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Post()
        {
            return Forbidden();
        }

        [HttpPut, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Put()
        {
            return Forbidden();
        }

        [HttpPatch, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Patch()
        {
            return Forbidden();
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            return Forbidden();
        }
    }
}
