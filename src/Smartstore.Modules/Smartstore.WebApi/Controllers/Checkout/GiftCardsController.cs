using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on GiftCard entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class GiftCardsController : WebApiController<GiftCard>
    {
        [HttpGet("GiftCards"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public IQueryable<GiftCard> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("GiftCards({key})"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public SingleResult<GiftCard> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("GiftCards({key})/GiftCardUsageHistory"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public IQueryable<GiftCardUsageHistory> GetGiftCardUsageHistory(int key)
        {
            return GetRelatedQuery(key, x => x.GiftCardUsageHistory);
        }

        [HttpGet("GiftCards({key})/PurchasedWithOrderItem"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<OrderItem> GetPurchasedWithOrderItem(int key)
        {
            return GetRelatedEntity(key, x => x.PurchasedWithOrderItem);
        }

        [HttpPost]
        [Permission(Permissions.Order.GiftCard.Update)]
        public Task<IActionResult> Post([FromBody] GiftCard model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Order.GiftCard.Update)]
        public Task<IActionResult> Put(int key, Delta<GiftCard> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.GiftCard.Update)]
        public Task<IActionResult> Patch(int key, Delta<GiftCard> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.GiftCard.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
