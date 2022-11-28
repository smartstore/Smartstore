using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Identity;
using Smartstore.Web.Api.Models.Checkout;

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
        public Task<IActionResult> Post([FromBody] Order model)
        {
            return PostAsync(model);
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
        [Produces(Json)]
        [ProducesResponseType(typeof(OrderShipmentInfo), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> GetShipmentInfo(int id)
        {
            try
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
        [Permission(Permissions.Order.Read)]
        [Produces(MediaTypeNames.Application.Pdf)]
        [ProducesResponseType(Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            try
            {
                var entity = await GetRequiredById(id, q => q
                    .Include(x => x.ShippingAddress)
                    .Include(x => x.BillingAddress)
                    .Include(x => x.Shipments));

                var stream = await _apiPdfHelper.Value.GeneratePdfAsync(entity);
                var fileName = _apiPdfHelper.Value.GetFileName(entity);

                return File(stream, MediaTypeNames.Application.Pdf, fileName);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Sets the payment status of an order to pending.
        /// </summary>
        [HttpPost("Orders({key})/PaymentPending"), ApiQueryable]
        [Permission(Permissions.Order.Update)]
        [Produces(Json)]
        [ProducesResponseType(typeof(Order), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> PaymentPending(int key)
        {
            try
            {
                var entity = await GetRequiredById(key);

                if (entity.PaymentStatus != PaymentStatus.Pending)
                {
                    entity.PaymentStatus = PaymentStatus.Pending;
                    await Db.SaveChangesAsync();
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Sets the payment status of an order to paid.
        /// </summary>
        /// <param name="paymentMethodName" example="Payments.PayPalStandard">The system name of a payment methid to be set.</param>
        [HttpPost("Orders({key})/PaymentPaid"), ApiQueryable]
        [Permission(Permissions.Order.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(Order), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> PaymentPaid(int key,
            [FromODataBody] string paymentMethodName)
        {
            try
            {
                var entity = await GetRequiredById(key);

                if (paymentMethodName != null && !entity.PaymentMethodSystemName.EqualsNoCase(paymentMethodName))
                {
                    entity.PaymentMethodSystemName = paymentMethodName;
                    await Db.SaveChangesAsync();
                }

                if (entity.CanMarkOrderAsPaid())
                {
                    await _orderProcessingService.Value.MarkOrderAsPaidAsync(entity);
                }
                
                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Refunds an order.
        /// </summary>
        /// <param name="online" example="false">
        /// A value indicating whether to refund online (refunding via payment provider) 
        /// or offline (just mark as refunded without calling the payment provider).
        /// </param>
        [HttpPost("Orders({key})/PaymentRefund"), ApiQueryable]
        [Permission(Permissions.Order.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(Order), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> PaymentRefund(int key,
            [FromODataBody, Required] bool online)
        {
            try
            {
                var entity = await GetRequiredById(key);

                if (online)
                {
                    if (await _orderProcessingService.Value.CanRefundAsync(entity))
                    {
                        var errors = await _orderProcessingService.Value.RefundAsync(entity);
                        if (errors.Any())
                        {
                            return ErrorResult(null, string.Join(". ", errors));
                        }
                    }
                }
                else
                {
                    if (entity.CanRefundOffline())
                    {
                        await _orderProcessingService.Value.RefundOfflineAsync(entity);
                    }
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Cancels an order.
        /// </summary>
        /// <param name="notifyCustomer">A value indicating whether to send a notification message to the customer about the cancelation.</param>
        [HttpPost("Orders({key})/Cancel"), ApiQueryable]
        [Permission(Permissions.Order.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(Order), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> Cancel(int key,
            [FromODataBody] bool notifyCustomer = true)
        {
            try
            {
                var entity = await GetRequiredById(key);
                if (entity.CanCancelOrder())
                {
                    await _orderProcessingService.Value.CancelOrderAsync(entity, notifyCustomer);
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Marks an order as completed.
        /// </summary>
        [HttpPost("Orders({key})/CompleteOrder"), ApiQueryable]
        [Permission(Permissions.Order.Update)]
        [Produces(Json)]
        [ProducesResponseType(typeof(Order), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> CompleteOrder(int key)
        {
            try
            {
                var entity = await GetRequiredById(key);
                if (entity.CanCompleteOrder())
                {
                    await _orderProcessingService.Value.CompleteOrderAsync(entity);
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Places items of an order into the shopping cart of the user who placed the order.
        /// </summary>
        [HttpPost("Orders({key})/ReOrder"), ApiQueryable]
        [Permission(Permissions.Order.Update)]
        [Produces(Json)]
        [ProducesResponseType(typeof(Order), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> ReOrder(int key)
        {
            try
            {
                var entity = await GetRequiredById(key);
                await _orderProcessingService.Value.ReOrderAsync(entity);

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Adds a shipment to an order.
        /// </summary>
        /// <param name="trackingNumber">Tracking number if any.</param>
        /// <param name="trackingUrl">Tracking URL if any.</param>
        /// <param name="isShipped">A value indicating whether to mark the shipment as shipped.</param>
        /// <param name="notifyCustomer">A value indicating whether to notify the customer that the shipment has been sent. Only applicable if **isShipped** is true.</param>
        /// <returns>New shipment or null if no shipment was added.</returns>
        [HttpPost("Orders({key})/AddShipment"), ApiQueryable]
        [Permission(Permissions.Order.EditShipment)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(Shipment), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> AddShipment(int key,
            [FromODataBody] string trackingNumber,
            [FromODataBody] string trackingUrl,
            [FromODataBody] bool isShipped = false,
            [FromODataBody] bool notifyCustomer = true)
        {
            try
            {
                var entity = await GetRequiredById(key);
                var shipment = await _orderProcessingService.Value.AddShipmentAsync(entity, trackingNumber, trackingUrl, null);
                if (shipment != null && isShipped)
                {
                    await _orderProcessingService.Value.ShipAsync(shipment, notifyCustomer);
                }

                return Ok(shipment);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
