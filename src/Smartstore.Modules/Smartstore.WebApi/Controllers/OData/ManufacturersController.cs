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

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public IQueryable<Manufacturer> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public SingleResult<Manufacturer> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public IQueryable<Discount> GetAppliedDiscounts(int key)
        {
            return GetRelatedQuery(key, x => x.AppliedDiscounts);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Manufacturer.Create)]
        public Task<IActionResult> Post([FromBody] Manufacturer model)
        {
            return PostAsync(model, async () =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(model);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Manufacturer.Update)]
        public Task<IActionResult> Put(int key, Delta<Manufacturer> model)
        {
            return PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Manufacturer.Update)]
        public Task<IActionResult> Patch(int key, Delta<Manufacturer> model)
        {
            return PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Manufacturer.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        private async Task UpdateSlug(Manufacturer entity)
        {
            var slugResult = await _urlService.Value.ValidateSlugAsync(entity, string.Empty, true);
            await _urlService.Value.ApplySlugAsync(slugResult, true);
        }
    }
}
