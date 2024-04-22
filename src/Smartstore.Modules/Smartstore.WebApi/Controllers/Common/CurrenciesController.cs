namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Currency entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class CurrenciesController : WebApiController<Currency>
    {
        [HttpGet("Currencies"), ApiQueryable]
        [Permission(Permissions.Configuration.Currency.Read)]
        public IQueryable<Currency> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Currencies({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Currency.Read)]
        public SingleResult<Currency> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Currency.Create)]
        public Task<IActionResult> Post([FromBody] Currency model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Currency.Update)]
        public Task<IActionResult> Put(int key, Delta<Currency> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Currency.Update)]
        public Task<IActionResult> Patch(int key, Delta<Currency> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Currency.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
