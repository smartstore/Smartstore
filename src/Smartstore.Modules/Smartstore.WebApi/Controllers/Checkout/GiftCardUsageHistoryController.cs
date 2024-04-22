using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on GiftCardUsageHistory entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class GiftCardUsageHistoryController : WebApiController<GiftCardUsageHistory>
    {
        [HttpGet("GiftCardUsageHistory"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public IQueryable<GiftCardUsageHistory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("GiftCardUsageHistory({key})"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public SingleResult<GiftCardUsageHistory> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("GiftCardUsageHistory({key})/GiftCard"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public SingleResult<GiftCard> GetGiftCard(int key)
        {
            return GetRelatedEntity(key, x => x.GiftCard);
        }

        [HttpGet("GiftCardUsageHistory({key})/UsedWithOrder"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> GetUsedWithOrder(int key)
        {
            return GetRelatedEntity(key, x => x.UsedWithOrder);
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
