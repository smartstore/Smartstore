namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on GenericAttribute entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Common)]
    public class GenericAttributesController : WebApiController<GenericAttribute>
    {
        [HttpGet("GenericAttributes"), ApiQueryable]
        public IQueryable<GenericAttribute> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("GenericAttributes({key})"), ApiQueryable]
        public SingleResult<GenericAttribute> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        public Task<IActionResult> Post([FromBody] GenericAttribute model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        public Task<IActionResult> Put(int key, Delta<GenericAttribute> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        public Task<IActionResult> Patch(int key, Delta<GenericAttribute> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
