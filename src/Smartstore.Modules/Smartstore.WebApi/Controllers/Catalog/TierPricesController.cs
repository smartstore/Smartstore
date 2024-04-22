using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on TierPrice entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class TierPricesController : WebApiController<TierPrice>
    {
        [HttpGet("TierPrices"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<TierPrice> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("TierPrices({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<TierPrice> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public Task<IActionResult> Post([FromBody] TierPrice model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public Task<IActionResult> Put(int key, Delta<TierPrice> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public Task<IActionResult> Patch(int key, Delta<TierPrice> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.EditTierPrice)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
