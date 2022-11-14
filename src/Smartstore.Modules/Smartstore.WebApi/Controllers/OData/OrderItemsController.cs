using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on OrderItem entity.
    /// </summary>
    public class OrderItemsController : WebApiController<OrderItem>
    {
        private readonly Lazy<IOrderProcessingService> _orderProcessingService;

        public OrderItemsController(Lazy<IOrderProcessingService> orderProcessingService)
        {
            _orderProcessingService = orderProcessingService;
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<OrderItem> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<OrderItem> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> GetOrder(int key)
        {
            return GetRelatedEntity(key, x => x.Order);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Product> GetProduct(int key)
        {
            return GetRelatedEntity(key, x => x.Product);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> Post([FromBody] OrderItem entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> Put(int key, Delta<OrderItem> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> Patch(int key, Delta<OrderItem> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }

        #region Actions and functions

        [HttpGet("OrderItems/GetShipmentInfo(id={id})")]
        [Permission(Permissions.Order.Read)]
        [ProducesResponseType(Status200OK)]
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
