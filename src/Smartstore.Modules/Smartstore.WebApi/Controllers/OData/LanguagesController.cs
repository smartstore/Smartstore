using Smartstore.Core.Localization;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Language entity.
    /// </summary>
    public class LanguagesController : WebApiController<Language>
    {
        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Language.Read)]
        public IQueryable<Language> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Configuration.Language.Read)]
        public SingleResult<Language> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.Create)]
        public async Task<IActionResult> Post([FromBody] Language entity)
        {
            return await PostAsync(entity);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Language.Update)]
        public async Task<IActionResult> Put(int key, Delta<Language> model)
        {
            return await PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Category.Update)]
        public async Task<IActionResult> Patch(int key, Delta<Language> model)
        {
            return await PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Language.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }
    }
}
