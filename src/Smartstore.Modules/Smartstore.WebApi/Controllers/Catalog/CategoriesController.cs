using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Category entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class CategoriesController : WebApiController<Category>
    {
        private readonly Lazy<ICategoryService> _categoryService;
        private readonly Lazy<IDiscountService> _discountService;

        public CategoriesController(
            Lazy<ICategoryService> categoryService,
            Lazy<IDiscountService> discountService)
        {
            _categoryService = categoryService;
            _discountService = discountService;
        }

        [HttpGet("Categories"), ApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public IQueryable<Category> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("Categories({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public SingleResult<Category> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("Categories({key})/MediaFile"), ApiQueryable]
        [Permission(Permissions.Catalog.Category.Read)]
        public SingleResult<MediaFile> GetMediaFile(int key)
        {
            return GetRelatedEntity(key, x => x.MediaFile);
        }

        [HttpGet("Categories({key})/AppliedDiscounts"), ApiQueryable]
        [Permission(Permissions.Promotion.Discount.Read)]
        public IQueryable<Discount> GetAppliedDiscounts(int key)
        {
            return GetRelatedQuery(key, x => x.AppliedDiscounts);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Category.Create)]
        public Task<IActionResult> Post([FromBody] Category model)
        {
            return PostAsync(model, async () =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlugAsync(model);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Category.Update)]
        public Task<IActionResult> Put(int key, Delta<Category> model)
        {
            return PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlugAsync(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Category.Update)]
        public Task<IActionResult> Patch(int key, Delta<Category> model)
        {
            return PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlugAsync(entity);
            });
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Category.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key, async (entity) =>
            {
                await _categoryService.Value.DeleteCategoryAsync(entity);
            });
        }

        /// <summary>
        /// Adds or removes discounts assigments.
        /// </summary>
        /// <remarks>
        /// Identifiers of discounts that are not included in **discountIds** are assigned to the category.
        /// Existing assignments to discounts that are not included in **discountIds** are removed.
        /// </remarks>
        /// <param name="discountIds">List of discount identifiers to apply.</param>
        [HttpPost("Categories({key})/ApplyDiscounts"), ApiQueryable]
        [Permission(Permissions.Catalog.Category.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<Discount>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> ApplyDiscounts(int key,
            [FromODataBody, Required] IEnumerable<int> discountIds)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.AppliedDiscounts));
                if (await _discountService.Value.ApplyDiscountsAsync(entity, discountIds.ToArray(), DiscountType.AssignedToCategories))
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
    }
}
