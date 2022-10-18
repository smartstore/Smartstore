using Smartstore.Core.Localization;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on LocalizedProperty entity.
    /// </summary>
    public class LocalizedPropertiesController : WebApiController<LocalizedProperty>
    {
        [HttpGet, WebApiQueryable]
        public IQueryable<LocalizedProperty> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        public SingleResult<LocalizedProperty> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, WebApiQueryable]
        public SingleResult<Language> GetLanguage(int key)
        {
            return GetRelatedEntity(key, x => x.Language);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LocalizedProperty entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        public async Task<IActionResult> Put(int key, Delta<LocalizedProperty> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        public async Task<IActionResult> Patch(int key, Delta<LocalizedProperty> model)
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
