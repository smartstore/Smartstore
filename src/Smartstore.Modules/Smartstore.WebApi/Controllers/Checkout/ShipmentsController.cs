using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Shipment entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class ShipmentsController : WebApiController<Shipment>
    {
        private readonly Lazy<IOrderProcessingService> _orderProcessingService;

        public ShipmentsController(Lazy<IOrderProcessingService> orderProcessingService)
        {
            _orderProcessingService = orderProcessingService;
        }

        [HttpGet("Shipments"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<Shipment> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Shipments({key})"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Shipment> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("Shipments({key})/ShipmentItems"), ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<ShipmentItem> GetShipmentItems(int key)
        {
            return GetRelatedQuery(key, x => x.ShipmentItems);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Post([FromBody] Shipment model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Put(int key, Delta<Shipment> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Patch(int key, Delta<Shipment> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        #region Actions and functions

        ///// <summary>
        ///// Downloads packaging slips as PDF.
        ///// </summary>
        ///// <param name="ids" example="[1,2,3]">Comma separated list of Shipment identifiers.</param>
        //[HttpGet("Shipments/DownloadPdfPackagingSlips(ids={ids})")]
        //[Permission(Permissions.Order.Read)]
        //[Produces(MediaTypeNames.Application.Pdf)]
        //[ProducesResponseType(Status200OK)]
        //[ProducesResponseType(Status404NotFound)]
        //[ProducesResponseType(Status422UnprocessableEntity)]
        //public async Task<IActionResult> DownloadPdfPackagingSlips([FromODataUri, Required] int[] ids)
        //{
        //    if (ids.IsNullOrEmpty())
        //    {
        //        return BadRequest("Shipment identifier(s) required.");
        //    }

        //    try
        //    {
        //        var entities = await Entities
        //            .Include(x => x.ShipmentItems)
        //            .Include(x => x.Order.ShippingAddress.Country)
        //            .Include(x => x.Order.ShippingAddress.StateProvince)
        //            .Where(x => ids.Contains(x.Id))
        //            .OrderByDescending(x => x.CreatedOnUtc)
        //            .ToListAsync();

        //        var (content, fileName) = await _orderHelper.Value.GeneratePdfPackagingSlipsAsync(entities);

        //        return File(content, MediaTypeNames.Application.Pdf, fileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ErrorResult(ex);
        //    }
        //}

        /// <summary>
        /// Marks a shipment as shipped.
        /// </summary>
        /// <param name="notifyCustomer">A value indicating whether to send a notification message to the customer.</param>
        [HttpPost("Shipments({key})/Ship"), ApiQueryable]
        [Permission(Permissions.Order.EditShipment)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(Shipment), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> Ship(int key,
            [FromODataBody] bool notifyCustomer = true)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.Order));
                if (!entity.ShippedDateUtc.HasValue)
                {
                    await _orderProcessingService.Value.ShipAsync(entity, notifyCustomer);
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Marks a shipment as delivered.
        /// </summary>
        /// <param name="notifyCustomer">A value indicating whether to send a notification message to the customer.</param>
        [HttpPost("Shipments({key})/Deliver"), ApiQueryable]
        [Permission(Permissions.Order.EditShipment)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(Shipment), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> Deliver(int key,
            [FromODataBody] bool notifyCustomer = true)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.Order));

                await _orderProcessingService.Value.DeliverAsync(entity, notifyCustomer);

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        #endregion
    }
}
