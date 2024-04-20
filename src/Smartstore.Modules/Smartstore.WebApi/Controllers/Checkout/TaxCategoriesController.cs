using Smartstore.Core.Checkout.Tax;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on TaxCategory entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Checkout)]
    public class TaxCategoriesController : WebApiController<TaxCategory>
    {
        [HttpGet("TaxCategories"), ApiQueryable]
        [Permission(Permissions.Configuration.Tax.Read)]
        public IQueryable<TaxCategory> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("TaxCategories({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Tax.Read)]
        public SingleResult<TaxCategory> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Create)]
        public Task<IActionResult> Post([FromBody] TaxCategory model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Tax.Update)]
        public Task<IActionResult> Put(int key, Delta<TaxCategory> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Tax.Update)]
        public Task<IActionResult> Patch(int key, Delta<TaxCategory> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Tax.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
