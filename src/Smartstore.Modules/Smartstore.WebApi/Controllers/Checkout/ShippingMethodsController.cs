using Smartstore.Core.Checkout.Shipping;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ShippingMethod entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class ShippingMethodsController : WebApiController<ShippingMethod>
    {
        [HttpGet("ShippingMethods"), ApiQueryable]
        [Permission(Permissions.Configuration.Shipping.Read)]
        public IQueryable<ShippingMethod> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ShippingMethods({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Shipping.Read)]
        public SingleResult<ShippingMethod> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Create)]
        public Task<IActionResult> Post([FromBody] ShippingMethod model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Shipping.Update)]
        public Task<IActionResult> Put(int key, Delta<ShippingMethod> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Shipping.Update)]
        public Task<IActionResult> Patch(int key, Delta<ShippingMethod> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Shipping.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
