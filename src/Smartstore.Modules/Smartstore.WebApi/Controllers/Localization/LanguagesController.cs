using Smartstore.Core.Localization;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Language entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Platform)]
    public class LanguagesController : WebApiController<Language>
    {
        [HttpGet("Languages"), ApiQueryable]
        [Permission(Permissions.Configuration.Language.Read)]
        public IQueryable<Language> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Languages({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Language.Read)]
        public SingleResult<Language> Get(int key)
        {
            return GetById(key);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.Create)]
        public Task<IActionResult> Post([FromBody] Language model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        [Permission(Permissions.Configuration.Language.Update)]
        public Task<IActionResult> Put(int key, Delta<Language> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Category.Update)]
        public Task<IActionResult> Patch(int key, Delta<Language> model)
        {
            return PatchAsync(key, model);
        }

        [HttpDelete]
        [Permission(Permissions.Configuration.Language.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }
    }
}
