using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Manufacturer entity.
    /// </summary>
    public class ManufacturersController : WebApiController<Manufacturer>
    {
        private readonly Lazy<IUrlService> _urlService;

        public ManufacturersController(Lazy<IUrlService> urlService)
        {
            _urlService = urlService;
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public IQueryable<Manufacturer> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public SingleResult<Manufacturer> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpGet, WebApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public IQueryable<Discount> GetAppliedDiscounts(int key)
        {
            return GetRelatedQuery(key, x => x.AppliedDiscounts);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Manufacturer.Create)]
        public async Task<IActionResult> Post([FromBody] Manufacturer entity)
        {
            return await PostAsync(entity, async () =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Manufacturer.Update)]
        public async Task<IActionResult> Put(int key, Delta<Manufacturer> model)
        {
            return await PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Manufacturer.Update)]
        public async Task<IActionResult> Patch(int key, Delta<Manufacturer> model)
        {
            return await PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Manufacturer.Delete)]
        public async Task<IActionResult> Delete(int key)
        {
            return await DeleteAsync(key);
        }

        private async Task UpdateSlug(Manufacturer entity)
        {
            var slugResult = await _urlService.Value.ValidateSlugAsync(entity, string.Empty, entity.Name, true);
            await _urlService.Value.ApplySlugAsync(slugResult, true);
        }
    }
}
