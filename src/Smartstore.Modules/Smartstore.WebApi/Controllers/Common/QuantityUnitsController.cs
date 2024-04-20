namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on QuantityUnit entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class QuantityUnitsController : WebApiController<QuantityUnit>
    {
        [HttpGet("QuantityUnits"), ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public IQueryable<QuantityUnit> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("QuantityUnits({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public SingleResult<QuantityUnit> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Create)]
        public Task<IActionResult> Post([FromBody] QuantityUnit model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Measure.Update)]
        public Task<IActionResult> Put(int key, Delta<QuantityUnit> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Measure.Update)]
        public Task<IActionResult> Patch(int key, Delta<QuantityUnit> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Measure.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
