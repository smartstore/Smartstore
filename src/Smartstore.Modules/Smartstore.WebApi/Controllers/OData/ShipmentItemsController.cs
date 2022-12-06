using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on ShipmentItem entity.
    /// </summary>
    public class ShipmentItemsController : WebApiController<ShipmentItem>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public IQueryable<ShipmentItem> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<ShipmentItem> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Order.Read)]
        public SingleResult<Shipment> GetShipment(int key)
        {
            return GetRelatedEntity(key, x => x.Shipment);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Post([FromBody] ShipmentItem model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Put(int key, Delta<ShipmentItem> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Patch(int key, Delta<ShipmentItem> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Order.EditShipment)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
