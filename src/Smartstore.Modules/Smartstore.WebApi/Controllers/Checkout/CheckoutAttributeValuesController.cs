using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on CheckoutAttributeValue entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class CheckoutAttributeValuesController : WebApiController<CheckoutAttributeValue>
    {
        [HttpGet("CheckoutAttributeValues"), ApiQueryable]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public IQueryable<CheckoutAttributeValue> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("CheckoutAttributeValues({key})"), ApiQueryable]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public SingleResult<CheckoutAttributeValue> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("CheckoutAttributeValues({key})/CheckoutAttribute"), ApiQueryable]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public SingleResult<CheckoutAttribute> GetCheckoutAttribute(int key)
        {
            return GetRelatedEntity(key, x => x.CheckoutAttribute);
        }

        [HttpGet("CheckoutAttributeValues({key})/MediaFile"), ApiQueryable]
        [Permission(Permissions.Cart.CheckoutAttribute.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpPost]
        [Permission(Permissions.Cart.CheckoutAttribute.Create)]
        public Task<IActionResult> Post([FromBody] CheckoutAttributeValue model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public Task<IActionResult> Put(int key, Delta<CheckoutAttributeValue> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Cart.CheckoutAttribute.Update)]
        public Task<IActionResult> Patch(int key, Delta<CheckoutAttributeValue> model)
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
