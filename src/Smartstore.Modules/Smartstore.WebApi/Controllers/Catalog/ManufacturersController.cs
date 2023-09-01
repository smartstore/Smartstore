using Microsoft.AspNetCore.OData.Formatter;
using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Manufacturer entity.
    /// </summary>
    public class ManufacturersController : WebApiController<Manufacturer>
    {
        private readonly Lazy<IUrlService> _urlService;
        private readonly Lazy<IDiscountService> _discountService;

        public ManufacturersController(
            Lazy<IUrlService> urlService,
            Lazy<IDiscountService> discountService)
        {
            _urlService = urlService;
            _discountService = discountService;
        }

        [HttpGet("Manufacturers"), ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public IQueryable<Manufacturer> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Manufacturers({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public SingleResult<Manufacturer> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("Manufacturers({key})/MediaFile"), ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpGet("Manufacturers({key})/AppliedDiscounts"), ApiQueryable]
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

        /// <summary>
        /// Adds or removes discounts assigments.
        /// </summary>
        /// <remarks>
        /// Identifiers of discounts that are not included in **discountIds** are assigned to the manufacturer.
        /// Existing assignments to discounts that are not included in **discountIds** are removed.
        /// </remarks>
        /// <param name="discountIds">List of discount identifiers to apply.</param>
        [HttpPost("Manufacturers({key})/ApplyDiscounts"), ApiQueryable]
        [Permission(Permissions.Catalog.Manufacturer.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<Discount>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> ApplyDiscounts(int key,
            [FromODataBody, Required] IEnumerable<int> discountIds)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.AppliedDiscounts));
                if (await _discountService.Value.ApplyDiscountsAsync(entity, discountIds.ToArray(), DiscountType.AssignedToManufacturers))
                {
                    await Db.SaveChangesAsync();
                }

                return Ok(entity.AppliedDiscounts.AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        private async Task UpdateSlug(Manufacturer entity)
        {
            var slugResult = await _urlService.Value.ValidateSlugAsync(entity, string.Empty, true);
            await _urlService.Value.ApplySlugAsync(slugResult, true);
        }
    }
}
