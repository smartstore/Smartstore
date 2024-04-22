using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Api.Models.Checkout;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on OrderItem entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class OrderItemsController : WebApiController<OrderItem>
    {
        private readonly Lazy<IOrderProcessingService> _orderProcessingService;

        public OrderItemsController(Lazy<IOrderProcessingService> orderProcessingService)
        {
            _orderProcessingService = orderProcessingService;
        }

        [HttpGet("OrderItems"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<OrderItem> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("OrderItems({key})"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<OrderItem> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("OrderItems({key})/Order"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> GetOrder(int key)
        {
            return GetRelatedEntity(key, x => x.Order);
        }

        [HttpGet("OrderItems({key})/Product"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Product> GetProduct(int key)
        {
            return GetRelatedEntity(key, x => x.Product);
        }

        [HttpGet("OrderItems({key})/AssociatedGiftCards"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public IQueryable<GiftCard> GetAssociatedGiftCards(int key)
        {
            return GetRelatedQuery(key, x => x.AssociatedGiftCards);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditItem)]
        public Task<IActionResult> Post([FromBody] OrderItem model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Order.EditItem)]
        public Task<IActionResult> Put(int key, Delta<OrderItem> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.EditItem)]
        public Task<IActionResult> Patch(int key, Delta<OrderItem> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.EditItem)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        #region Actions and functions

        /// <summary>
        /// Gets additional shipment information for an order item.
        /// </summary>
        [HttpGet("OrderItems/GetShipmentInfo(id={id})")]
        [Permission(Permissions.Order.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(OrderItemShipmentInfo), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> GetShipmentInfo(int id)
        {
            var entity = await Entities
                .AsSplitQuery()
                .Include(x => x.Order.Shipments)
                .ThenInclude(x => x.ShipmentItems)
                .FindByIdAsync(id);

            if (entity == null)
            {
                return NotFound(id);
            }

            try
            {
                var service = _orderProcessingService.Value;
                var result = new OrderItemShipmentInfo
                {
                    ItemsCanBeAddedToShipmentCount = await service.GetShippableItemsCountAsync(entity),
                    ShipmentItemsCount = await service.GetShipmentItemsCountAsync(entity),
                    DispatchedItemsCount = await service.GetDispatchedItemsCountAsync(entity, true),
                    NotDispatchedItemsCount = await service.GetDispatchedItemsCountAsync(entity, false),
                    DeliveredItemsCount = await service.GetDeliveredItemsCountAsync(entity, true),
                    NotDeliveredItemsCount = await service.GetDeliveredItemsCountAsync(entity, false),
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
