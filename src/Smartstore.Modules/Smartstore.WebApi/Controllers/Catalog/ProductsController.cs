#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Net.Http.Headers;
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
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Domain;
using Smartstore.IO;
using Smartstore.Web.Api.Models.Catalog;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on Product entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Catalog)]
    public class ProductsController : WebApiController<Product>
    {
        private readonly Lazy<ICatalogSearchService> _catalogSearchService;
        private readonly Lazy<ICatalogSearchQueryFactory> _catalogSearchQueryFactory;
        private readonly Lazy<IPriceCalculationService> _priceCalculationService;
        private readonly Lazy<IProductAttributeService> _productAttributeService;
        private readonly Lazy<IProductTagService> _productTagService;
        private readonly Lazy<IProductService> _productService;
        private readonly Lazy<IDiscountService> _discountService;
        private readonly Lazy<IMediaImporter> _mediaImporter;
        private readonly Lazy<IWebApiService> _webApiService;
        private readonly Lazy<SearchSettings> _searchSettings;

        public ProductsController(
            Lazy<ICatalogSearchService> catalogSearchService,
            Lazy<ICatalogSearchQueryFactory> catalogSearchQueryFactory,
            Lazy<IPriceCalculationService> priceCalculationService,
            Lazy<IProductAttributeService> productAttributeService,
            Lazy<IProductTagService> productTagService,
            Lazy<IProductService> productService,
            Lazy<IDiscountService> discountService,
            Lazy<IMediaImporter> mediaImporter,
            Lazy<IWebApiService> webApiService,
            Lazy<SearchSettings> searchSettings)
        {
            _catalogSearchService = catalogSearchService;
            _catalogSearchQueryFactory = catalogSearchQueryFactory;
            _priceCalculationService = priceCalculationService;
            _productAttributeService = productAttributeService;
            _productTagService = productTagService;
            _productService = productService;
            _discountService = discountService;
            _mediaImporter = mediaImporter;
            _webApiService = webApiService;
            _searchSettings = searchSettings;
        }

        [HttpGet("Products"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<Product> Get()
        {
            // INFO: unlike in Classic, also returns system products. Someone may well use them for their own purposes.
            return Entities.AsNoTracking();
        }

        [HttpGet("Products({key})"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Product> Get(int key)
        {
            return GetById(key);
        }

        [HttpGet("Products({key})/DeliveryTime"), ApiQueryable]
        [Permission(Permissions.Configuration.DeliveryTime.Read)]
        public SingleResult<DeliveryTime> GetDeliveryTime(int key)
        {
            return GetRelatedEntity(key, x => x.DeliveryTime);
        }

        [HttpGet("Products({key})/QuantityUnit"), ApiQueryable]
        [Permission(Permissions.Configuration.Measure.Read)]
        public SingleResult<QuantityUnit> GetQuantityUnit(int key)
        {
            return GetRelatedEntity(key, x => x.QuantityUnit);
        }

        [HttpGet("Products({key})/CountryOfOrigin"), ApiQueryable]
        [Permission(Permissions.Configuration.Country.Read)]
        public SingleResult<Country> GetCountryOfOrigin(int key)
        {
            return GetRelatedEntity(key, x => x.CountryOfOrigin);
        }

        [HttpGet("Products({key})/SampleDownload"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public SingleResult<Download> GetSampleDownload(int key)
        {
            return GetRelatedEntity(key, x => x.SampleDownload);
        }

        [HttpGet("Products({key})/ProductCategories"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductCategory> GetProductCategories(int key)
        {
            return GetRelatedQuery(key, x => x.ProductCategories);
        }

        [HttpGet("Products({key})/ProductManufacturers"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductManufacturer> GetProductManufacturers(int key)
        {
            return GetRelatedQuery(key, x => x.ProductManufacturers);
        }

        [HttpGet("Products({key})/ProductMediaFiles"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductMediaFile> GetProductMediaFiles(int key)
        {
            return GetRelatedQuery(key, x => x.ProductMediaFiles);
        }

        [HttpGet("Products({key})/ProductSpecificationAttributes"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductSpecificationAttribute> GetProductSpecificationAttributes(int key)
        {
            return GetRelatedQuery(key, x => x.ProductSpecificationAttributes);
        }

        [HttpGet("Products({key})/ProductTags"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductTag> GetProductTags(int key)
        {
            return GetRelatedQuery(key, x => x.ProductTags);
        }

        [HttpGet("Products({key})/TierPrices"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<TierPrice> GetTierPrices(int key)
        {
            return GetRelatedQuery(key, x => x.TierPrices);
        }

        [HttpGet("Products({key})/AppliedDiscounts"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<Discount> GetAppliedDiscounts(int key)
        {
            return GetRelatedQuery(key, x => x.AppliedDiscounts);
        }

        [HttpGet("Products({key})/ProductVariantAttributes"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttribute> GetProductVariantAttributes(int key)
        {
            return GetRelatedQuery(key, x => x.ProductVariantAttributes);
        }

        [HttpGet("Products({key})/ProductVariantAttributeCombinations"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        public IQueryable<ProductVariantAttributeCombination> GetProductVariantAttributeCombinations(int key)
        {
            return GetRelatedQuery(key, x => x.ProductVariantAttributeCombinations);
        }

        [HttpGet("Products({key})/ProductBundleItems"), ApiQueryable]
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
                await UpdateSlugAsync(model);
            });
        }

        [HttpPut]
        [Permission(Permissions.Catalog.Product.Update)]
        public Task<IActionResult> Put(int key, Delta<Product> model)
        {
            return PutAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlugAsync(entity);
            });
        }

        [HttpPatch]
        [Permission(Permissions.Catalog.Product.Update)]
        public Task<IActionResult> Patch(int key, Delta<Product> model)
        {
            return PatchAsync(key, model, async (entity) =>
            {
                await Db.SaveChangesAsync();
                await UpdateSlugAsync(entity);
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
        /// Adds or removes assigments to product tags.
        /// </summary>
        /// <remarks>
        /// Tags that are not included in **tagNames** are added and assigned to the product.
        /// Existing assignments to tags that are not included in **tagNames** are removed.
        /// </remarks>
        /// <param name="tagNames">List of tag names to apply.</param>
        [HttpPost("Products({key})/UpdateProductTags"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<ProductTag>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateProductTags(int key,
        [FromODataBody, Required] IEnumerable<string> tagNames)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.ProductTags));
                await _productTagService.Value.UpdateProductTagsAsync(entity, tagNames);

                return Ok(entity.ProductTags.AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Adds or removes discounts assigments.
        /// </summary>
        /// <remarks>
        /// Identifiers of discounts that are not included in **discountIds** are assigned to the product.
        /// Existing assignments to discounts that are not included in **discountIds** are removed.
        /// </remarks>
        /// <param name="discountIds">List of discount identifiers to apply.</param>
        [HttpPost("Products({key})/ApplyDiscounts"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<Discount>), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> ApplyDiscounts(int key,
            [FromODataBody, Required] IEnumerable<int> discountIds)
        {
            try
            {
                var entity = await GetRequiredById(key, q => q.Include(x => x.AppliedDiscounts));
                if (await _discountService.Value.ApplyDiscountsAsync(entity, discountIds.ToArray(), DiscountType.AssignedToSkus))
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
            [FromODataBody, Required] IEnumerable<ManagedProductAttribute> attributes,
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

        /// <summary>
        /// Saves files like images and assigns them to a product.
        /// </summary>
        /// <param name="key">
        /// Identifier of the product to which the images should be assigned.
        /// 0 if the product is to be identified by SKU, GTIN or MPN.
        /// </param>
        /// <param name="files">The files to be saved.</param>
        /// <param name="sku">SKU (stock keeping unit) of the product to which the images should be assigned.</param>
        /// <param name="gtin">GTIN (global trade item number) of the product to which the images should be assigned.</param>
        /// <param name="mpn">MPN (manufacturer part number) of the product to which the images should be assigned.</param>
        /// <remarks>
        /// It does not matter if one of the uploaded images already exists. The Web API automatically ensures that a product 
        /// has no duplicate images by comparing both binary data streams.
        /// 
        /// It is also possible to update/replace an existing image. To do so simply add the file identifier as **fileId** attribute
        /// in the content disposition header of the file.
        /// </remarks>
        [HttpPost("Products({key})/SaveFiles"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.EditPicture)]
        [Consumes("multipart/form-data"), Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<ProductMediaFile>), Status200OK)]
        [ProducesResponseType(Status415UnsupportedMediaType)]
        public async Task<IActionResult> SaveFiles(int key,
            [Required] IFormFileCollection files,
            [FromQuery] string? sku = null,
            [FromQuery] string? gtin = null,
            [FromQuery] string? mpn = null)
        {
            if (!HasMultipartContent)
            {
                return StatusCode(Status415UnsupportedMediaType);
            }

            try
            {
                // INFO: "files" is just for Swagger upload. For generic clients it is empty.
                files = Request.Form.Files;
                if (files.Count == 0)
                {
                    return BadRequest("Missing multipart file data.");
                }

                var entity = (Product?)null;
                var query = Entities
                    .Include(x => x.ProductMediaFiles)
                    .ThenInclude(x => x.MediaFile);

                if (key != 0)
                {
                    entity = await query.FirstOrDefaultAsync(x => x.Id == key);
                }
                else if (sku.HasValue())
                {
                    entity = await query.ApplySkuFilter(sku).FirstOrDefaultAsync();
                }
                else if (gtin.HasValue())
                {
                    entity = await query.ApplyGtinFilter(gtin).FirstOrDefaultAsync();
                }
                else if (mpn.HasValue())
                {
                    entity = await query.ApplyMpnFilter(mpn).FirstOrDefaultAsync();
                }

                if (entity == null)
                {
                    return NotFound($"Cannot find {nameof(Product)} entity. Please specify a valid ID, SKU, GTIN or MPN.");
                }

                var items = files
                    .Select(file =>
                    {
                        Dictionary<string, object>? state = null;

                        if (ContentDispositionHeaderValue.TryParse(file.ContentDisposition, out var cd))
                        {
                            var fileId = cd.GetParameterValue<int>("fileId");
                            if (fileId != 0)
                            {
                                state = new Dictionary<string, object>
                                {
                                    { nameof(ProductMediaFile.MediaFileId), fileId }
                                };
                            }
                        }

                        return new FileBatchSource(MediaStorageItem.FromFormFile(file))
                        {
                            FileName = PathUtility.SanitizeFileName(file.FileName.EmptyNull() ?? System.IO.Path.GetRandomFileName())!,
                            State = state
                        };
                    })
                    .ToList();

                _mediaImporter.Value.MessageHandler = (msg, item) =>
                {
                    if (msg.MessageType == ImportMessageType.Error)
                    {
                        throw new Exception(msg.Message);
                    }
                };

                _ = await _mediaImporter.Value.ImportProductImagesAsync(entity, items);

                return Ok(entity.ProductMediaFiles.AsQueryable());
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

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
            // INFO: "EnsureStableOrdering" must be "false" otherwise CatalogSearchQuery. Sorting is getting lost.
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
                    .UseHitsFactory((set, ids) => Entities.SelectSummary().GetManyAsync(ids, true));

                var searchResult = await _catalogSearchService.Value.SearchAsync(searchQuery);
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
        /// Gets the soft-deleted products of the recycle bin.
        /// </summary>
        /// <remarks>
        /// Can only be used in conjunction with the methods **Restore** and **DeletePermanent** because soft-deleted products are excluded from other endpoints.
        /// </remarks>
        [HttpGet("Products/RecycleBin"), ApiQueryable]
        [Permission(Permissions.Catalog.Product.Read)]
        [Produces(Json)]
        [ProducesResponseType(typeof(IQueryable<Product>), Status200OK)]
        public IQueryable<Product> RecycleBin()
        {
            return Entities
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(x => x.Deleted);
        }

        /// <summary>
        /// Permanently deletes soft-deleted products.
        /// </summary>
        /// <param name="productIds">
        /// Identifiers of products to be permanently deleted.
        /// Use an empty list to delete all soft-deleted products (empty recycle bin).
        /// </param>
        [HttpPost("Products/DeletePermanent")]
        [Permission(Permissions.Catalog.Product.Delete)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(DeletionResult), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> DeletePermanent([FromODataBody, Required] IEnumerable<int> productIds)
        {
            try
            {
                if (!productIds.Any())
                {
                    productIds = await Db.Products
                        .IgnoreQueryFilters()
                        .Where(x => x.Deleted)
                        .Select(x => x.Id)
                        .ToArrayAsync();
                }

                var result = await _productService.Value.DeleteProductsPermanentAsync(productIds.ToArray());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Restores soft-deleted products.
        /// </summary>
        /// <param name="productIds">Identifiers of products to restore.</param>
        /// <param name="publishAfterRestore">A value indicating whether to publish restored products.</param>
        /// <returns>Number of restored products.</returns>
        [HttpPost("Products/Restore")]
        [Permission(Permissions.Catalog.Product.Create)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(int), Status200OK)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> Restore(
            [FromODataBody, Required] IEnumerable<int> productIds,
            [FromODataBody] bool? publishAfterRestore = null)
        {
            try
            {
                var result = await _productService.Value.RestoreProductsAsync(productIds.ToArray(), publishAfterRestore);
                return Ok(result);
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

        #endregion
    }
}
