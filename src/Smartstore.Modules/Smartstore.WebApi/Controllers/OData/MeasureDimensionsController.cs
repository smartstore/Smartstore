using Smartstore.Core.Common;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on MeasureDimension entity.
    /// </summary>
    public class MeasureDimensionsController : WebApiController<MeasureDimension>
    {
        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public IQueryable<MeasureDimension> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public SingleResult<MeasureDimension> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Create)]
        public async Task<IActionResult> Post([FromBody] MeasureDimension entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> Put(int key, Delta<MeasureDimension> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> Patch(int key, Delta<MeasureDimension> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Measure.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
