using Smartstore.Core.Checkout.Attributes;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on CheckoutAttribute entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class CheckoutAttributesController : WebApiController<CheckoutAttribute>
    {
        [HttpGet("CheckoutAttributes"), ApiQueryable]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public IQueryable<CheckoutAttribute> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("CheckoutAttributes({key})"), ApiQueryable]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public SingleResult<CheckoutAttribute> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("CheckoutAttributes({key})/CheckoutAttributeValues"), ApiQueryable]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public IQueryable<CheckoutAttributeValue> GetCheckoutAttributeValues(int key)
        {
            return GetRelatedQuery(key, x => x.CheckoutAttributeValues);
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Create)]
        public Task<IActionResult> Post([FromBody] CheckoutAttribute model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public Task<IActionResult> Put(int key, Delta<CheckoutAttribute> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public Task<IActionResult> Patch(int key, Delta<CheckoutAttribute> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Cart.CheckoutAttribute.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
