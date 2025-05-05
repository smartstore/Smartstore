using Smartstore.Core.Localization;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on LocaleStringResource entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Platform)]
    public class LocaleStringResourcesController : WebApiController<LocaleStringResource>
    {
        [HttpGet("LocaleStringResources"), ApiQueryable]
        public IQueryable<LocaleStringResource> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("LocaleStringResources({key})"), ApiQueryable]
        public SingleResult<LocaleStringResource> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("LocaleStringResources({key})/Language"), ApiQueryable]
        public SingleResult<Language> GetLanguage(int key)
        {
            return GetRelatedEntity(key, x => x.Language);
        }

        [HttpPost]
        public Task<IActionResult> Post([FromBody] LocaleStringResource model)
        {
            return PostAsync(model);
        }

        [HttpPut]
        public Task<IActionResult> Put(int key, Delta<LocaleStringResource> model)
        {
            return PutAsync(key, model);
        }

        [HttpPatch]
        public Task<IActionResult> Patch(int key, Delta<LocaleStringResource> model)
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
