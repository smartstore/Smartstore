using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on PriceLabel entity.
    /// </summary>
    public class PriceLabelsController : WebApiController<PriceLabel>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.PriceLabel.Read)]
        public IQueryable<PriceLabel> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.PriceLabel.Read)]
        public SingleResult<PriceLabel> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.PriceLabel.Create)]
        public async Task<IActionResult> Post([FromBody] PriceLabel entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.PriceLabel.Update)]
        public async Task<IActionResult> Put(int key, Delta<PriceLabel> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.PriceLabel.Update)]
        public async Task<IActionResult> Patch(int key, Delta<PriceLabel> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.PriceLabel.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
