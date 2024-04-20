using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on DiscountUsageHistory entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class DiscountUsageHistoryController : WebApiController<DiscountUsageHistory>
    {
        [HttpGet("DiscountUsageHistory"), ApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public IQueryable<DiscountUsageHistory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("DiscountUsageHistory({key})"), ApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public SingleResult<DiscountUsageHistory> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("DiscountUsageHistory({key})/Discount"), ApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public SingleResult<Discount> GetDiscount(int key)
        {
            return GetRelatedEntity(key, x => x.Discount);
        }

        [HttpGet("DiscountUsageHistory({key})/Order"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> GetOrder(int key)
        {
            return GetRelatedEntity(key, x => x.Order);
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
