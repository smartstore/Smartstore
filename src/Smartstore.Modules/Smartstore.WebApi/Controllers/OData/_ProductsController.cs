using Microsoft.AspNetCore.Http;
using Microsoft.OData;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Product entity.
    /// </summary>
    public class ProductsController : WebApiController<Product>
    {
        private readonly Lazy<IUrlService> _urlService;

        public ProductsController(Lazy<IUrlService> urlService)
        {
            _urlService = urlService;
        }

        // INFO: unlike in Classic, also returns system products. Someone may well use them for their own purposes.

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<Product> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Product> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        public SingleResult<DeliveryTime> GetDeliveryTime(int key)
        {
            return GetRelatedEntity(key, x => x.DeliveryTime);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public SingleResult<QuantityUnit> GetQuantityUnit(int key)
        {
            return GetRelatedEntity(key, x => x.QuantityUnit);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<Country> GetCountryOfOrigin(int key)
        {
            return GetRelatedEntity(key, x => x.CountryOfOrigin);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Download> GetSampleDownload(int key)
        {
            return GetRelatedEntity(key, x => x.SampleDownload);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductCategory> GetProductCategories(int key)
        {
            return GetRelatedQuery(key, x => x.ProductCategories);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductManufacturer> GetProductManufacturers(int key)
        {
            return GetRelatedQuery(key, x => x.ProductManufacturers);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductMediaFile> GetProductPictures(int key)
        {
            return GetRelatedQuery(key, x => x.ProductPictures);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductSpecificationAttribute> GetProductSpecificationAttributes(int key)
        {
            return GetRelatedQuery(key, x => x.ProductSpecificationAttributes);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductTag> GetProductTags(int key)
        {
            return GetRelatedQuery(key, x => x.ProductTags);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<TierPrice> GetTierPrices(int key)
        {
            return GetRelatedQuery(key, x => x.TierPrices);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<Discount> GetAppliedDiscounts(int key)
        {
            return GetRelatedQuery(key, x => x.AppliedDiscounts);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttribute> GetProductVariantAttributes(int key)
        {
            return GetRelatedQuery(key, x => x.ProductVariantAttributes);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttributeCombination> GetProductVariantAttributeCombinations(int key)
        {
            return GetRelatedQuery(key, x => x.ProductVariantAttributeCombinations);
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductBundleItem> GetProductBundleItems(int key)
        {
            return GetRelatedQuery(key, x => x.ProductBundleItems);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Product.Create)]
        public Task<IActionResult> Post([FromBody] Product model)
        {
            return PostAsync(model, async () =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(model);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.Update)]
        public Task<IActionResult> Put(int key, Delta<Product> model)
        {
            return PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.Update)]
        public Task<IActionResult> Patch(int key, Delta<Product> model)
        {
            return PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlug(entity);
            });
        }

        [HttpDelete]
        [Permission(Permissions.Catalog.Product.Delete)]
        public Task<IActionResult> Delete(int key)
        {
            return DeleteAsync(key);
        }

        [HttpPost("Products({key})/ProductCategories({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(ProductCategory), Status200OK)]
        [ProducesResponseType(typeof(ProductCategory), Status201Created)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> PostProductCategories(int key, 
            int relatedkey /*categoryId*/, 
            [FromBody] ProductCategory model)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.ProductCategories));
                var productCategory = entity.ProductCategories.FirstOrDefault(x => x.CategoryId == relatedkey);
                if (productCategory == null)
                {
                    // No assignment yet.

                    // TODO: apply model...

                    return Created(productCategory);
                }

                return Ok(productCategory);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }



        private async Task UpdateSlug(Product entity)
        {
            var slugResult = await _urlService.Value.ValidateSlugAsync(entity, string.Empty, true);
            await _urlService.Value.ApplySlugAsync(slugResult, true);
        }
    }
}
