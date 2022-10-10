using Smartstore.Core.Catalog.Discounts;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Discount entity.
    /// </summary>
    public class DiscountsController : WebApiController<Discount>
    {
        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public IQueryable<Discount> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public Task<IActionResult> Get(int key)
        {
            return GetByIdAsync(key);
        }

        [HttpGet("discounts({key})/{property}")]
        [HttpGet("discounts/{key}/{property}")]
        [Permission(Permissions.Promotion.Discount.Read)]
        public Task<IActionResult> GetProperty(int key, string property)
        {
            return GetPropertyValueAsync(key, property);
        }

        [HttpPost]
        [Permission(Permissions.Promotion.Discount.Create)]
        public async Task<IActionResult> Post([FromBody] Discount entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Promotion.Discount.Update)]
        public async Task<IActionResult> Put(int key, Delta<Discount> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Promotion.Discount.Update)]
        public async Task<IActionResult> Patch(int key, Delta<Discount> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Promotion.Discount.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
