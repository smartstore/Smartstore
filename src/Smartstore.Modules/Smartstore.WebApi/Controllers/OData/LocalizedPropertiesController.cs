using Smartstore.Core.Localization;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on LocalizedProperty entity.
    /// </summary>
    public class LocalizedPropertiesController : WebApiController<LocalizedProperty>
    {
        [HttpGet, ApiQueryable]
        public IQueryable<LocalizedProperty> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        public SingleResult<LocalizedProperty> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        public SingleResult<Language> GetLanguage(int key)
        {
            return GetRelatedEntity(key, x => x.Language);
        }

        [HttpPost]
        public Task<IActionResult> Post([FromBody] LocalizedProperty model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        public Task<IActionResult> Put(int key, Delta<LocalizedProperty> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        public Task<IActionResult> Patch(int key, Delta<LocalizedProperty> model)
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
