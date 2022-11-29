#nullable enable

using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
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
using Smartstore.Web.Api.Models.Catalog;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on Product entity.
    /// </summary>
    public class ProductsController : WebApiController<Product>
    {
        private readonly Lazy<IUrlService> _urlService;
        private readonly Lazy<ICatalogSearchService> _catalogSearchService;
        private readonly Lazy<ICatalogSearchQueryFactory> _catalogSearchQueryFactory;
        private readonly Lazy<IPriceCalculationService> _priceCalculationService;
        private readonly Lazy<IProductAttributeService> _productAttributeService;
        private readonly Lazy<IWebApiService> _webApiService;
        private readonly Lazy<SearchSettings> _searchSettings;

        public ProductsController(
            Lazy<IUrlService> urlService,
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ICatalogSearchQueryFactory> catalogSearchQueryFactory,
            Lazy<IPriceCalculationService> priceCalculationService,
            Lazy<IProductAttributeService> productAttributeService,
            Lazy<IWebApiService> webApiService,
            Lazy<SearchSettings> searchSettings)
        {
            _urlService = urlService;
            _catalogSearchService = catalogSearchService;
            _catalogSearchQueryFactory = catalogSearchQueryFactory;
            _priceCalculationService = priceCalculationService;
            _productAttributeService = productAttributeService;
            _webApiService = webApiService;
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
            return GetRelatedQuery(key, x => x.ProductMediaFiles);
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
        [HttpPost("Products({key})/ProductMediaFiles({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(ProductMediaFile), Status200OK)]
        [ProducesResponseType(typeof(ProductMediaFile), Status201Created)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> PostProductMediaFiles(int key,
            int relatedkey /*mediaFileId*/,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ProductMediaFile? model = default)
        {
            model ??= new();
            model.ProductId = key;
            model.MediaFileId = relatedkey;

            return AddRelatedEntity(key,
                model,
                x => x.ProductMediaFiles,
                x => x.MediaFileId == relatedkey);
        }

        /// <summary>
        /// Deletes a ProductMediaFile.
        /// </summary>
        /// <param name="relatedkey" example="345">The media file identifier. 0 to remove all media file assignments for the product.</param>
        [HttpDelete("Products({key})/ProductMediaFiles({relatedkey})")]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        [ProducesResponseType(Status204NoContent)]
        [ProducesResponseType(Status404NotFound)]
        public Task<IActionResult> DeleteProductMediaFiles(int key, int relatedkey /*mediaFileId*/)
        {
            return RemoveRelatedEntities(key,
                relatedkey,
                x => x.ProductMediaFiles,
                x => x.MediaFileId == relatedkey);
        }

        #region Actions and functions

        /// <summary>
        /// Searches for products.
        /// </summary>
        [HttpPost("Products/Search")]
        [ApiQueryable(AllowedQueryOptions = AllowedQueryOptions.Expand | AllowedQueryOptions.Count | AllowedQueryOptions.Select, EnsureStableOrdering = false)]
        [Permission(Permissions.Catalog.Product.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<Product>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> Search([FromQuery] CatalogSearchQueryModel model)
        {
            // INFO: "Search" needs to be POST otherwise "... is not a valid OData path template. Bad Request - Error in query syntax".
            // INFO: "EnsureStableOrdering" must be "false" otherwise CatalogSearchQuery.Sorting is getting lost.
            // INFO: we cannot fully satisfy both: catalog search options and OData query options. Catalog search has priority here.

            try
            {
                if (model.Term == null || model.Term.Length < _searchSettings.Value.InstantSearchTermMinLength)
                {
                    return BadRequest($"The minimum length for the search term is {_searchSettings.Value.InstantSearchTermMinLength} characters.");
                }

                var state = _webApiService.Value.GetState();
                var searchQuery = await _catalogSearchQueryFactory.Value.CreateFromQueryAsync();

                searchQuery = searchQuery
                    .BuildFacetMap(false)
                    .CheckSpelling(0)
                    .Slice(searchQuery.Skip, Math.Min(searchQuery.Take, state.MaxTop))
                    .UseHitsFactory((set, ids) => Entities.GetManyAsync(ids, true));

                var searchResult = await _catalogSearchService.Value.SearchAsync(searchQuery);

                // TODO: (mg) (core) using "$expand" results in "Compiling a query which loads related collections for more than one collection navigation...
                // but no 'QuerySplittingBehavior' has been configured."
                // Applying "AsSplitQuery" if "$expand" is used might be solution but test with $select if it also works together.

                //$"term:{model.Term.NaIfEmpty()} skip:{searchQuery.Skip} take:{searchQuery.Take} hits:{searchResult.HitsEntityIds.Length} total:{searchResult.TotalHitsCount}".Dump();

                var hits = await searchResult.GetHitsAsync();

                return Ok(hits.AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Calculates a product price.
        /// </summary>
        /// <param name="forListing" example="false">
        /// A value indicating whether to calculate the price for a product list.
        /// Speeds up the calculation if true, since lowest and presselected price are not calculated.
        /// </param>
        /// <param name="quantity" example="1">The product quantity. 1 by default.</param>
        /// <param name="customerId">The identifier of a customer to calculate the price for. Obtained from IWorkContext.CurrentCustomer if 0.</param>
        /// <param name="targetCurrencyId">The target currency to use for money conversion. Obtained from IWorkContext.WorkingCurrency if 0.</param>
        [HttpPost("Products({key})/CalculatePrice")]
        [Permission(Permissions.Catalog.Product.Read)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(CalculatedProductPrice), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> CalculatePrice(int key,
            [FromODataBody] bool forListing = false,
            [FromODataBody] int quantity = 1,
            [FromODataBody] int customerId = 0,
            [FromODataBody] int targetCurrencyId = 0)
        {
            try
            {
                var entity = await GetRequiredById(key);
                var customer = await Db.Customers.FindByIdAsync(customerId);
                var targetCurrency = await Db.Currencies.FindByIdAsync(targetCurrencyId);

                var calculationOptions = _priceCalculationService.Value.CreateDefaultOptions(forListing, customer, targetCurrency);
                var p = await _priceCalculationService.Value.CalculatePriceAsync(new PriceCalculationContext(entity, quantity, calculationOptions));

                var result = new CalculatedProductPrice
                {
                    ProductId = key,
                    CurrencyId = p.FinalPrice.Currency.Id,
                    CurrencyCode = p.FinalPrice.Currency.CurrencyCode,
                    FinalPrice = p.FinalPrice.Amount,
                    RegularPrice = p.RegularPrice?.Amount,
                    RetailPrice = p.RetailPrice?.Amount,
                    OfferPrice = p.OfferPrice?.Amount,
                    ValidUntilUtc = p.ValidUntilUtc,
                    PreselectedPrice = p.PreselectedPrice?.Amount,
                    LowestPrice = p.LowestPrice?.Amount,
                    DiscountAmount = p.DiscountAmount.Amount,
                    Saving = new()
                    {
                        HasSaving = p.Saving.HasSaving,
                        SavingPrice = p.Saving.SavingPrice.Amount,
                        SavingPercent = p.Saving.SavingPercent,
                        SavingAmount = p.Saving.SavingAmount?.Amount
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Creates all variant attributes combinations for a product.
        /// Already existing combinations will be deleted before.
        /// </summary>
        [HttpPost("Products({key})/CreateAttributeCombinations"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        [Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<ProductVariantAttributeCombination>), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> CreateAttributeCombinations(int key)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.ProductVariantAttributeCombinations));
                await _productAttributeService.Value.CreateAllAttributeCombinationsAsync(key);

                return Ok(entity.ProductVariantAttributeCombinations.AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Manages\synchronizes the attributes and attribute values of a product.
        /// </summary>
        /// <param name="attributes">The attributes and attribute values to be processed.</param>
        /// <param name="synchronize">
        /// If set to false, only missing attributes and attribute values are inserted.
        /// If set to true, existing records are also updated and values not included in the request body are removed from the database.
        /// This means that if no attribute values are sent, then the attribute is removed along with all values for this product.
        /// </param>
        [HttpPost("Products({key})/ManageAttributes"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.EditVariant)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<ProductVariantAttribute>), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> ManageAttributes(int key,
            [FromODataBody] IEnumerable<ManagedProductAttribute> attributes,
            [FromODataBody] bool synchronize = false)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.ProductVariantAttributes).ThenInclude(x => x.ProductAttribute));

                var toDeleteValueIds = new HashSet<int>();
                var names = new HashSet<string>(attributes.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
                var existingAttributes = await Db.ProductAttributes
                    .Where(x => names.Contains(x.Name))
                    .Select(x => new { x.Id, x.Name })
                    .ToListAsync();
                var existingAttributesDic = existingAttributes.ToDictionarySafe(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase);

                // Add missing attributes.
                var missingAttributes = attributes
                    .Where(x => !existingAttributesDic.ContainsKey(x.Name))
                    .Select(x => new ProductAttribute { Name = x.Name })
                    .ToArray();
                if (missingAttributes.Length > 0)
                {
                    Db.ProductAttributes.AddRange(missingAttributes);
                    await Db.SaveChangesAsync();

                    missingAttributes.Each(x => existingAttributesDic[x.Name] = x.Id);
                }

                // Product attribute mappings.
                var existingMappings = entity.ProductVariantAttributes
                    .ToDictionarySafe(x => x.ProductAttribute.Name, x => x, StringComparer.OrdinalIgnoreCase);

                foreach (var item in attributes)
                {
                    if (!existingMappings.TryGetValue(item.Name, out var productAttribute))
                    {
                        // No attribute mapping yet.
                        var isEmptyAttribute = synchronize && item.Values.Count == 0;
                        if (!isEmptyAttribute)
                        {
                            productAttribute = new ProductVariantAttribute
                            {
                                ProductId = entity.Id,
                                ProductAttributeId = existingAttributesDic[item.Name],
                                IsRequired = item.IsRequired,
                                AttributeControlTypeId = (int)item.ControlType,
                                CustomData = item.CustomData,
                                DisplayOrder = entity.ProductVariantAttributes
                                    .OrderByDescending(x => x.DisplayOrder)
                                    .Select(x => x.DisplayOrder)
                                    .FirstOrDefault() + 1
                            };

                            entity.ProductVariantAttributes.Add(productAttribute);
                            existingMappings[item.Name] = productAttribute;
                        }
                    }
                    else if (synchronize)
                    {
                        // Has already an attribute mapping.
                        if (item.Values.Count == 0 && productAttribute.IsListTypeAttribute())
                        {
                            Db.ProductVariantAttributes.Remove(productAttribute);
                        }
                        else
                        {
                            productAttribute.IsRequired = item.IsRequired;
                            productAttribute.AttributeControlTypeId = (int)item.ControlType;
                            productAttribute.CustomData = item.CustomData;
                        }
                    }
                }

                await Db.SaveChangesAsync();

                // Product attribute values.
                foreach (var item in attributes)
                {
                    if (existingMappings.TryGetValue(item.Name, out var productAttribute))
                    {
                        var maxDisplayOrder = productAttribute.ProductVariantAttributeValues
                            .OrderByDescending(x => x.DisplayOrder)
                            .Select(x => x.DisplayOrder)
                            .FirstOrDefault();

                        var existingValues = productAttribute.ProductVariantAttributeValues
                            .ToDictionarySafe(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

                        foreach (var val in item.Values.Where(x => x.Name.HasValue()))
                        {
                            if (!existingValues.TryGetValue(val.Name, out var value))
                            {
                                value = new ProductVariantAttributeValue
                                {
                                    ProductVariantAttributeId = productAttribute.Id,
                                    Name = val.Name,
                                    Alias = val.Alias,
                                    Color = val.Color,
                                    PriceAdjustment = val.PriceAdjustment,
                                    WeightAdjustment = val.WeightAdjustment,
                                    IsPreSelected = val.IsPreSelected,
                                    DisplayOrder = ++maxDisplayOrder
                                };

                                productAttribute.ProductVariantAttributeValues.Add(value);
                                existingValues[val.Name] = value;
                            }
                            else if (synchronize)
                            {
                                value.Alias = val.Alias;
                                value.Color = val.Color;
                                value.PriceAdjustment = val.PriceAdjustment;
                                value.WeightAdjustment = val.WeightAdjustment;
                                value.IsPreSelected = val.IsPreSelected;
                            }
                        }

                        if (synchronize)
                        {
                            var ids = productAttribute.ProductVariantAttributeValues
                                .Where(value => !item.Values.Any(x => x.Name.EqualsNoCase(value.Name)))
                                .Select(x => x.Id);

                            toDeleteValueIds.AddRange(ids);
                        }
                    }
                }

                await Db.SaveChangesAsync();

                // Deletes values not present in sent values.
                // Separate step to avoid DbUpdateConcurrencyException.
                if (toDeleteValueIds.Count > 0)
                {
                    await Db.ProductVariantAttributeValues
                        .Where(x => toDeleteValueIds.Contains(x.Id))
                        .ExecuteDeleteAsync();
                }

                return Ok(entity.ProductVariantAttributes.AsQueryable());
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
