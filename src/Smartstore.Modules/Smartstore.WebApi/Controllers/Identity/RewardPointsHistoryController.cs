using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on RewardPointsHistory entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Identity)]
    public class RewardPointsHistoryController : WebApiController<RewardPointsHistory>
    {
        [HttpGet("RewardPointsHistory"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public IQueryable<RewardPointsHistory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("RewardPointsHistory({key})"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<RewardPointsHistory> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("RewardPointsHistory({key})/UsedWithOrder"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Order> GetUsedWithOrder(int key)
        {
            return GetRelatedEntity(key, x => x.UsedWithOrder);
        }

        [HttpGet("RewardPointsHistory({key})/Customer"), ApiQueryable]
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
