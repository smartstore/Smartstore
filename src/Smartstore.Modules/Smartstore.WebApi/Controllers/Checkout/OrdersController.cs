using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Configuration;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Web.Api.Models.Checkout;
using Smartstore.Web.Controllers;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Order entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class OrdersController : WebApiController<Order>
    {
        private readonly Lazy<IOrderProcessingService> _orderProcessingService;
        private readonly Lazy<ISettingFactory> _settingFactory;
        private readonly Lazy<OrderHelper> _orderHelper;
        private readonly Lazy<IRoundingHelper> _roundingHelper;
        private readonly IWorkContext _workContext;

        public OrdersController(
            Lazy<IOrderProcessingService> orderProcessingService,
            Lazy<ISettingFactory> settingFactory,
            Lazy<OrderHelper> orderHelper,
            Lazy<IRoundingHelper> roundingHelper,
            IWorkContext workContext)
        {
            _orderProcessingService = orderProcessingService;
            _settingFactory = settingFactory;
            _orderHelper = orderHelper;
            _roundingHelper = roundingHelper;
            _workContext = workContext;
        }

        [HttpGet("Orders"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<Order> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Orders({key})"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Order> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("Orders({key})/Customer"), ApiQueryable]
        [Permission(Permissions.Customer.Read)]
        public SingleResult<Customer> GetCustomer(int key)
        {
            return GetRelatedEntity(key, x => x.Customer);
        }

        [HttpGet("Orders({key})/BillingAddress"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Address> GetBillingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.BillingAddress);
        }

        [HttpGet("Orders({key})/ShippingAddress"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Address> GetShippingAddress(int key)
        {
            return GetRelatedEntity(key, x => x.ShippingAddress);
        }

        /// <summary>
        /// Gets WalletHistory entities assigned to an Order.
        /// </summary>
        /// <remarks>Only applicable if a wallet plugin is installed.</remarks>
        [HttpGet("Orders({key})/WalletHistory"), ApiQueryable]
        [Permission("Wallet.read")]
        public IQueryable<WalletHistory> GetWalletHistory(int key)
        {
            return GetRelatedQuery(key, x => x.WalletHistory);
        }

        [HttpGet("Orders({key})/DiscountUsageHistory"), ApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public IQueryable<DiscountUsageHistory> GetDiscountUsageHistory(int key)
        {
            return GetRelatedQuery(key, x => x.DiscountUsageHistory);
        }

        [HttpGet("Orders({key})/GiftCardUsageHistory"), ApiQueryable]
        [Permission(Permissions.Order.GiftCard.Read)]
        public IQueryable<GiftCardUsageHistory> GetGiftCardUsageHistory(int key)
        {
            return GetRelatedQuery(key, x => x.GiftCardUsageHistory);
        }

        [HttpGet("Orders({key})/OrderNotes"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<OrderNote> GetOrderNotes(int key)
        {
            return GetRelatedQuery(key, x => x.OrderNotes);
        }

        [HttpGet("Orders({key})/Shipments"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<Shipment> GetShipments(int key)
        {
            return GetRelatedQuery(key, x => x.Shipments);
        }

        [HttpGet("Orders({key})/OrderItems"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<OrderItem> GetOrderItems(int key)
        {
            return GetRelatedQuery(key, x => x.OrderItems);
        }

        [HttpGet("Orders({key})/RedeemedRewardPointsEntry"), ApiQueryable]
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
        /// Gets prepared order details like the SKU or image URL of the ordered product variant.
        /// </summary>
        [HttpGet("Orders/GetDetails(id={id})")]
        [Permission(Permissions.Order.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(OrderDetails), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> GetDetails(int id)
        {
            IActionResult result = null;
            Language workingLanguage = null;

            try
            {
                var entity = await Entities
                    .Include(x => x.ShippingAddress)
                    .Include(x => x.BillingAddress)
                    .Include(x => x.OrderItems)
                    .ThenInclude(x => x.Product)
                    .FindByIdAsync(id);

                if (entity == null)
                {
                    return NotFound(id);
                }

                if (entity.CustomerLanguageId != _workContext.WorkingLanguage.Id)
                {
                    // Get details in context of language that was applied at the time the order was placed.
                    workingLanguage = _workContext.WorkingLanguage;
                    _workContext.WorkingLanguage = await Db.Languages.FindByIdAsync(entity.CustomerLanguageId, false);
                }

                var model = await _orderHelper.Value.PrepareOrderDetailsModelAsync(entity);

                var items = model.Items
                    .Select(x => new OrderDetails.OrderItemDetails
                    {
                        OrderItemId = x.Id,
                        ProductId = x.ProductId,
                        Sku = x.Sku,
                        ProductName = x.ProductName,
                        ProductSlug = x.ProductSeName,
                        ProductUrl = x.ProductUrl,
                        UnitPrice = _roundingHelper.Value.Round(x.UnitPrice),
                        SubTotal = _roundingHelper.Value.Round(x.SubTotal),
                        ProductImageUrl = x.Image?.Url,
                        ProductThumbUrl = x.Image?.ThumbUrl
                    })
                    .ToList();

                var details = new OrderDetails
                {
                    Id = entity.Id,
                    ShowSku = model.ShowSku,
                    ShowProductImages = model.ShowProductImages,
                    OrderTotal = _roundingHelper.Value.Round(model.OrderTotal),
                    OrderSubtotal = _roundingHelper.Value.Round(model.OrderSubtotal),
                    Items = items
                };

                result = Ok(details);
            }
            catch (Exception ex)
            {
                result = ErrorResult(ex);
            }
            finally
            {
                if (workingLanguage != null)
                {
                    _workContext.WorkingLanguage = workingLanguage;
                }
            }

            return result;
        }

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

                var (content, fileName) = await _orderHelper.Value.GeneratePdfAsync(new[] { entity });

                return File(content, MediaTypeNames.Application.Pdf, fileName);
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
        /// Captures the order amount to be paid.
        /// </summary>
        [HttpPost("Orders({key})/PaymentCapture"), ApiQueryable]
        [Permission(Permissions.Order.Update)]
        [Produces(Json)]
        [ProducesResponseType(typeof(Order), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> PaymentCapture(int key)
        {
            try
            {
                var entity = await GetRequiredById(key);

                if (await _orderProcessingService.Value.CanCaptureAsync(entity))
                {
                    await _orderProcessingService.Value.CaptureAsync(entity);
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Refunds the paid amount.
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
                // TODO: (mg) decimal parameter "amountToRefund" (FromODataBody) always 0.
                decimal amountToRefund = 0;

                var entity = await GetRequiredById(key);

                if (online)
                {
                    if (amountToRefund == 0)
                    {
                        if (await _orderProcessingService.Value.CanRefundAsync(entity))
                        {
                            await _orderProcessingService.Value.RefundAsync(entity);
                        }
                    }
                    else
                    {
                        if (await _orderProcessingService.Value.CanPartiallyRefundAsync(entity, amountToRefund))
                        {
                            await _orderProcessingService.Value.PartiallyRefundAsync(entity, amountToRefund);
                        }
                    }
                }
                else
                {
                    if (amountToRefund == 0)
                    {
                        if (entity.CanRefundOffline())
                        {
                            await _orderProcessingService.Value.RefundOfflineAsync(entity);
                        }
                    }
                    else
                    {
                        if (entity.CanPartiallyRefundOffline(amountToRefund))
                        {
                            await _orderProcessingService.Value.PartiallyRefundOfflineAsync(entity, amountToRefund);
                        }
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
        [ProducesResponseType(Status403Forbidden)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> ReOrder(int key)
        {
            try
            {
                var entity = await GetRequiredById(key);
                var orderSettings = await _settingFactory.Value.LoadSettingsAsync<OrderSettings>(entity.StoreId);

                if (!orderSettings.IsReOrderAllowed)
                {
                    return Forbidden($"Reorder is forbidden due to setting {nameof(OrderSettings)}.{nameof(OrderSettings.IsReOrderAllowed)} for store with ID {entity.StoreId}.");
                }

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
