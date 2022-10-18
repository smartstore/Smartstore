using Smartstore.Core.Common;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on GenericAttribute entity.
    /// </summary>
    public class GenericAttributesController : WebApiController<GenericAttribute>
    {
        [HttpGet, WebApiQueryable]
        public IQueryable<GenericAttribute> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        public SingleResult<GenericAttribute> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GenericAttribute entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        public async Task<IActionResult> Put(int key, Delta<GenericAttribute> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(int key, Delta<GenericAttribute> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
