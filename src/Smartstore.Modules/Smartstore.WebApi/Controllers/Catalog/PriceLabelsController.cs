using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on PriceLabel entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class PriceLabelsController : WebApiController<PriceLabel>
    {
        [HttpGet("PriceLabels"), ApiQueryable]
        [Permission(Permissions.Configuration.PriceLabel.Read)]
        public IQueryable<PriceLabel> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("PriceLabels({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.PriceLabel.Read)]
        public SingleResult<PriceLabel> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.PriceLabel.Create)]
        public Task<IActionResult> Post([FromBody] PriceLabel model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.PriceLabel.Update)]
        public Task<IActionResult> Put(int key, Delta<PriceLabel> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.PriceLabel.Update)]
        public Task<IActionResult> Patch(int key, Delta<PriceLabel> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.PriceLabel.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
