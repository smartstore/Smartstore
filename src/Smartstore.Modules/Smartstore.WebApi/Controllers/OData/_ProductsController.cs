#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Catalog.Search.Modelling;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Seo;
using Smartstore.Domain;

namespace Smartstore.Web.Api.Controllers.OData
{
    // TODO: (mg) (core) naming consistency. After next release, rename navigation property Product.ProductPictures to Product.ProductMediaFiles.
    // Otherwise we get OData path template warnings for e.g. 'odata/v1/Products({key})/ProductMediaFiles({relatedkey})'.

    /// <summary>
    /// The endpoint for operations on Product entity.
    /// </summary>
    public class ProductsController : WebApiController<Product>
    {
        private readonly Lazy<IUrlService> _urlService;
        private readonly Lazy<ICatalogSearchService> _catalogSearchService;
        private readonly Lazy<ICatalogSearchQueryFactory> _catalogSearchQueryFactory;
        private readonly Lazy<SearchSettings> _searchSettings;

        public ProductsController(
            Lazy<IUrlService> urlService,
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ICatalogSearchQueryFactory> catalogSearchQueryFactory,
            Lazy<SearchSettings> searchSettings)
        {
            _urlService = urlService;
            _catalogSearchService = catalogSearchService;
            _catalogSearchQueryFactory = catalogSearchQueryFactory;
            _searchSettings = searchSettings;
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<Product> Get()
        {
            // INFO: unlike in Classic, also returns system products. Someone may well use them for their own purposes.
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
        public IQueryable<ProductMediaFile> GetProductMediaFiles(int key)
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

        /// <summary>
        /// Creates a ProductCategory (assignment of a Product to a Category).
        /// </summary>
        /// <param name="relatedkey" example="123">The category identifier.</param>
        [HttpPost("Products({key})/ProductCategories({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(ProductCategory), Status200OK)]
        [ProducesResponseType(typeof(ProductCategory), Status201Created)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> PostProductCategories(int key, 
            int relatedkey /*categoryId*/, 
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ProductCategory? model = default)
        {
            model ??= new();
            model.ProductId = key;
            model.CategoryId = relatedkey;

            return AddRelatedEntity(key,
                model,
                x => x.ProductCategories,
                x => x.CategoryId == relatedkey);
        }

        /// <summary>
        /// Deletes a ProductCategory.
        /// </summary>
        /// <param name="relatedkey" example="123">The category identifier. 0 to remove all category assignments for the product.</param>
        [HttpDelete("Products({key})/ProductCategories({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditCategory)]
        [ProducesResponseType(Status204NoContent)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> DeleteProductCategories(int key, int relatedkey /*categoryId*/)
        {
            return RemoveRelatedEntities(key,
                relatedkey,
                x => x.ProductCategories,
                x => x.CategoryId == relatedkey);
        }

        /// <summary>
        /// Creates a ProductManufacturer (assignment of a Product to a Manufacturer).
        /// </summary>
        /// <param name="relatedkey" example="234">The manufacturer identifier.</param>
        [HttpPost("Products({key})/ProductManufacturers({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(ProductManufacturer), Status200OK)]
        [ProducesResponseType(typeof(ProductManufacturer), Status201Created)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> PostProductManufacturers(int key,
            int relatedkey /*manufacturerId*/,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ProductManufacturer? model = default)
        {
            model ??= new();
            model.ProductId = key;
            model.ManufacturerId = relatedkey;

            return AddRelatedEntity(key,
                model,
                x => x.ProductManufacturers,
                x => x.ManufacturerId == relatedkey);
        }

        /// <summary>
        /// Deletes a ProductManufacturer.
        /// </summary>
        /// <param name="relatedkey" example="234">The manufacturer identifier. 0 to remove all manufacturer assignments for the product.</param>
        [HttpDelete("Products({key})/ProductManufacturers({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditManufacturer)]
        [ProducesResponseType(Status204NoContent)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> DeleteProductManufacturers(int key, int relatedkey /*manufacturerId*/)
        {
            return RemoveRelatedEntities(key,
                relatedkey,
                x => x.ProductManufacturers,
                x => x.ManufacturerId == relatedkey);
        }

        /// <summary>
        /// Creates a ProductMediaFile (assignment of a Product to a MediaFile).
        /// </summary>
        /// <param name="relatedkey" example="345">The media file identifier.</param>
        [HttpPost("Products({key})/ProductPictures({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(ProductMediaFile), Status200OK)]
        [ProducesResponseType(typeof(ProductMediaFile), Status201Created)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> PostProductPictures(int key,
            int relatedkey /*mediaFileId*/,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ProductMediaFile? model = default)
        {
            model ??= new();
            model.ProductId = key;
            model.MediaFileId = relatedkey;

            return AddRelatedEntity(key,
                model,
                x => x.ProductPictures,
                x => x.MediaFileId == relatedkey);
        }

        /// <summary>
        /// Deletes a ProductMediaFile.
        /// </summary>
        /// <param name="relatedkey" example="345">The media file identifier. 0 to remove all media file assignments for the product.</param>
        [HttpDelete("Products({key})/ProductPictures({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        [ProducesResponseType(Status204NoContent)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> DeleteProductPictures(int key, int relatedkey /*mediaFileId*/)
        {
            return RemoveRelatedEntities(key,
                relatedkey,
                x => x.ProductPictures,
                x => x.MediaFileId == relatedkey);
        }

        #region Actions and functions

        /// <summary>
        /// Searches for products.
        /// </summary>
        /// <param name="q" example="iphone">The search term.</param>
        [HttpGet("Products/Search"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<Product>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> Search(
            [FromQuery, Required] string q)// TODO: (mg) (core) add and comment all search parameters :-(
        {
            try
            {
                if (q == null || q.Length < _searchSettings.Value.InstantSearchTermMinLength)
                {
                    return BadRequest($"The minimum length for the search term is {_searchSettings.Value.InstantSearchTermMinLength} characters.");
                }

                var query = await _catalogSearchQueryFactory.Value.CreateFromQueryAsync();
                var result = await _catalogSearchService.Value.SearchAsync(query);
                var hits = await result.GetHitsAsync();

                $"hits:{hits.Count} term:{query.Term}".Dump();

                return Ok(hits.AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }


        #endregion

        #region Utilities

        private async Task<IActionResult> AddRelatedEntity<TProperty>(int key,
            TProperty model,
            Expression<Func<Product, ICollection<TProperty>>> navProperty,
            Func<TProperty, bool> itemPredicate)
            where TProperty : BaseEntity
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var entity = await GetRequiredById(key, q => q.Include(navProperty));
                var func = navProperty.Compile();
                var collection = func(entity);
                var relatedEntity = collection.FirstOrDefault(itemPredicate);

                if (relatedEntity == null)
                {
                    Db.Set<TProperty>().Add(model);
                    await Db.SaveChangesAsync();

                    return Created(model);
                }

                return Ok(relatedEntity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        private async Task<IActionResult> RemoveRelatedEntities<TProperty>(int key,
            int relatedkey,
            Expression<Func<Product, ICollection<TProperty>>> navProperty,
            Func<TProperty, bool> itemPredicate)
            where TProperty : BaseEntity
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(navProperty));
                var func = navProperty.Compile();
                var collection = func(entity);

                if (collection.Count > 0)
                {
                    if (relatedkey == 0)
                    {
                        Db.Set<TProperty>().RemoveRange(collection);
                        await Db.SaveChangesAsync();
                    }
                    else
                    {
                        var relatedEntity = collection.FirstOrDefault(itemPredicate);
                        if (relatedEntity != null)
                        {
                            Db.Set<TProperty>().Remove(relatedEntity);
                            await Db.SaveChangesAsync();
                        }
                    }
                }

                return NoContent();
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

        #endregion
    }
}
