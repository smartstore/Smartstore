namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on MeasureDimension entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class MeasureDimensionsController : WebApiController<MeasureDimension>
    {
        [HttpGet("MeasureDimensions"), ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public IQueryable<MeasureDimension> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("MeasureDimensions({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public SingleResult<MeasureDimension> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Create)]
        public Task<IActionResult> Post([FromBody] MeasureDimension model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Measure.Update)]
        public Task<IActionResult> Put(int key, Delta<MeasureDimension> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Measure.Update)]
        public Task<IActionResult> Patch(int key, Delta<MeasureDimension> model)
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
