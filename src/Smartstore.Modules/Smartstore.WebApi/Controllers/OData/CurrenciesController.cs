using Smartstore.Core.Common;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Currency entity.
    /// </summary>
    public class CurrenciesController : WebApiController<Currency>
    {
        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Currency.Read)]
        public IQueryable<Currency> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Currency.Read)]
        public SingleResult<Currency> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Create)]
        public async Task<IActionResult> Post([FromBody] Currency entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Currency.Update)]
        public async Task<IActionResult> Put(int key, Delta<Currency> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Currency.Update)]
        public async Task<IActionResult> Patch(int key, Delta<Currency> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Currency.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
