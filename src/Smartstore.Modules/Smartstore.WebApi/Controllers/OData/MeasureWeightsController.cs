using Smartstore.Core.Common;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on MeasureWeight entity.
    /// </summary>
    public class MeasureWeightsController : WebApiController<MeasureWeight>
    {
        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public IQueryable<MeasureWeight> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public SingleResult<MeasureWeight> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Create)]
        public async Task<IActionResult> Post([FromBody] MeasureWeight entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> Put(int key, Delta<MeasureWeight> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> Patch(int key, Delta<MeasureWeight> model)
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
