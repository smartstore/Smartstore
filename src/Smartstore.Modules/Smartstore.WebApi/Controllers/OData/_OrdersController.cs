using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Order entity.
    /// </summary>
    public class OrdersController : WebApiController<Order>
    {
        private readonly Lazy<IOrderProcessingService> _orderProcessingService;
        private readonly Lazy<ApiPdfHelper> _apiPdfHelper;

        public OrdersController(
            Lazy<IOrderProcessingService> orderProcessingService,
            Lazy<ApiPdfHelper> apiPdfHelper)
        {
            _orderProcessingService = orderProcessingService;
            _apiPdfHelper = apiPdfHelper;
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<Order> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> Get(int key)
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
        [Permission(Permissions.Order.Read)]
        public SingleResult<Address> GetBillingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.BillingAddress);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Address> GetShippingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.ShippingAddress);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<OrderNote> GetOrderNotes(int key)
        {
            return GetRelatedQuery(key, x => x.OrderNotes);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<Shipment> GetShipments(int key)
        {
            return GetRelatedQuery(key, x => x.Shipments);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<OrderItem> GetOrderItems(int key)
        {
            return GetRelatedQuery(key, x => x.OrderItems);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<RewardPointsHistory> GetRedeemedRewardPointsEntry(int key)
        {
            return GetRelatedEntity(key, x => x.RedeemedRewardPointsEntry);
        }

        [HttpPost]
        [Permission(Permissions.Order.Create)]
        public Task<IActionResult> Post([FromBody] Order entity)
        {
            return PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Order.Update)]
        public Task<IActionResult> Put(int key, Delta<Order> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.Update)]
        public Task<IActionResult> Patch(int key, Delta<Order> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        #region Actions and functions

        /// <summary>
        /// Gets additional shipment information for an order.
        /// </summary>
        [HttpGet("Orders/GetShipmentInfo(id={id})")]
        [Permission(Permissions.Order.Read)]
        [ProducesResponseType(typeof(OrderShipmentInfo), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> GetShipmentInfo(int id)
        {
            var entity = await Entities
                .AsSplitQuery()
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .Include(x => x.Shipments)
                .ThenInclude(x => x.ShipmentItems)
                .FindByIdAsync(id);

            if (entity == null)
            {
                return NotFound(id);
            }

            try
            {
                var service = _orderProcessingService.Value;
                var result = new OrderShipmentInfo
                {
                    HasItemsToDispatch = await _orderProcessingService.Value.HasItemsToDispatchAsync(entity),
                    HasItemsToDeliver = await _orderProcessingService.Value.HasItemsToDeliverAsync(entity),
                    CanAddItemsToShipment = await _orderProcessingService.Value.CanAddItemsToShipmentAsync(entity)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Downloads an order as a PDF.
        /// </summary>
        [HttpGet("Orders/DownloadPdf(id={id})")]
        [Produces(MediaTypeNames.Application.Pdf)]
        [Permission(Permissions.Order.Read)]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var entity = await Entities
                .Include(x => x.ShippingAddress)
                .Include(x => x.BillingAddress)
                .Include(x => x.Shipments)
                .FindByIdAsync(id);

            if (entity == null)
            {
                return NotFound(id);
            }

            try
            {
                var stream = await _apiPdfHelper.Value.GeneratePdfAsync(entity);
                var fileName = _apiPdfHelper.Value.GetFileName(entity);

                return File(stream, MediaTypeNames.Application.Pdf, fileName);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
