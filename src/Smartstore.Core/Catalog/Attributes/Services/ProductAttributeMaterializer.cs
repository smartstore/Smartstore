using System.Text;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Utilities;

namespace Smartstore.Core.Catalog.Attributes
{
    public partial class ProductAttributeMaterializer : IProductAttributeMaterializer
    {
        // 0 = Attribute IDs
        private const string ATTRIBUTES_BY_IDS_KEY = "materialized-attributes:{0}";
        private const string ATTRIBUTES_PATTERN_KEY = "materialized-attributes:*";

        // 0 = Attribute JSON
        private const string ATTRIBUTEVALUES_BY_JSON_KEY = "materialized-attributevalues:byjson-{0}";
        private const string ATTRIBUTEVALUES_PATTERN_KEY = "materialized-attributevalues:*";

        // 0 = ProductId, 1 = Attribute JSON
        private const string ATTRIBUTECOMBINATION_BY_IDJSON_KEY = "attributecombination:byjson-{0}-{1}";
        internal const string ATTRIBUTECOMBINATION_PATTERN_KEY = "attributecombination:*";

        // 0 = ProductId
        internal const string UNAVAILABLE_COMBINATIONS_KEY = "attributecombination:unavailable-{0}";
        internal const string UNAVAILABLE_COMBINATIONS_PATTERN_KEY = "attributecombination:unavailable-*";

        private readonly SmartDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRequestCache _requestCache;
        private readonly ICacheManager _cache;
        private readonly Lazy<IDownloadService> _downloadService;
        private readonly Lazy<CatalogSettings> _catalogSettings;
        private readonly PerformanceSettings _performanceSettings;

        public ProductAttributeMaterializer(
            SmartDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IRequestCache requestCache,
            ICacheManager cache,
            Lazy<IDownloadService> downloadService,
            Lazy<CatalogSettings> catalogSettings,
            PerformanceSettings performanceSettings)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _requestCache = requestCache;
            _cache = cache;
            _downloadService = downloadService;
            _catalogSettings = catalogSettings;
            _performanceSettings = performanceSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// All attribute control types to which <see cref="ProductVariantAttributeValue"/> entities can be assigned.
        /// </summary>
        internal readonly static int[] AttributeListControlTypeIds = new[]
        {
            (int)AttributeControlType.DropdownList,
            (int)AttributeControlType.RadioList,
            (int)AttributeControlType.Checkboxes,
            (int)AttributeControlType.Boxes
        };

        public virtual async Task<int> PrefetchProductVariantAttributesAsync(IEnumerable<ProductVariantAttributeSelection> selections)
        {
            Guard.NotNull(selections, nameof(selections));

            if (!selections.Any())
            {
                return 0;
            }

            // Determine uncached attributes.
            var alreadyCollectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var infos = new List<AttributeSelectionInfo>();

            foreach (var selection in selections.Where(x => x.AttributesMap.Any()))
            {
                var key = ATTRIBUTEVALUES_BY_JSON_KEY.FormatInvariant(selection.AsJson());

                if (!alreadyCollectedKeys.Contains(key) && !_requestCache.Contains(key))
                {
                    infos.Add(new AttributeSelectionInfo(selection, key));
                    alreadyCollectedKeys.Add(key);
                }
            }

            var allAttributeIds = infos.SelectMany(x => x.AttributeIds)
                .Distinct()
                .ToArray();

            var allValues = infos.SelectMany(x => x.Values)
                .Distinct()
                .ToArray();

            // Load all values in one go.
            var attributeValues = await LoadAttributeValuesAsync(allAttributeIds, allValues);
            var attributeValuesMap = attributeValues.ToDictionarySafe(x => x.Id);

            // Create a single cache entry for each passed attribute selection.
            foreach (var info in infos)
            {
                var cachedValues = new List<ProductVariantAttributeValue>();

                // Ensure value id order in cached result list is correct.
                foreach (var value in info.Values)
                {
                    if (attributeValuesMap.TryGetValue(value, out var attributeValue))
                    {
                        cachedValues.Add(attributeValue);
                    }
                }

                // Put it in cache.
                _requestCache.Put(info.ValuesCacheKey, cachedValues);
            }

            return infos.Count;
        }

        public virtual async Task<IList<ProductVariantAttribute>> MaterializeProductVariantAttributesAsync(ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var ids = selection.AttributesMap.Select(x => x.Key).ToArray();
            if (!ids.Any())
            {
                return new List<ProductVariantAttribute>();
            }

            var cacheKey = ATTRIBUTES_BY_IDS_KEY.FormatInvariant(string.Join(",", ids));

            var result = await _requestCache.GetAsync(cacheKey, async () =>
            {
                var query = _db.ProductVariantAttributes
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Include(x => x.ProductAttribute)
                    .Include(x => x.ProductVariantAttributeValues)
                    .Where(x => ids.Contains(x.Id))
                    .OrderBy(x => x.DisplayOrder);

                var attributes = await query.ToListAsync();

                return attributes.OrderBySequence(ids).ToList();
            });

            return result;
        }

        public virtual async Task<IList<ProductVariantAttributeValue>> MaterializeProductVariantAttributeValuesAsync(ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(selection, nameof(selection));

            var cacheKey = ATTRIBUTEVALUES_BY_JSON_KEY.FormatInvariant(selection.AsJson());

            var result = await _requestCache.GetAsync(cacheKey, async () =>
            {
                var attributeIds = selection.AttributesMap.Select(x => x.Key).ToArray();
                var values = GetIntegerValues(selection);

                return await LoadAttributeValuesAsync(attributeIds, values);
            });

            return result;
        }

        public virtual async Task<(ProductVariantAttributeSelection Selection, List<string> Warnings)> CreateAttributeSelectionAsync(
            ProductVariantQuery query,
            IEnumerable<ProductVariantAttribute> attributes,
            int productId,
            int bundleItemId,
            bool getFilesFromRequest = true)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(attributes, nameof(attributes));

            var selection = new ProductVariantAttributeSelection(null);
            var warnings = new List<string>();

            foreach (var pva in attributes)
            {
                var selectedItems = query.Variants.Where(x =>
                    x.ProductId == productId &&
                    x.BundleItemId == bundleItemId &&
                    x.AttributeId == pva.ProductAttributeId &&
                    x.VariantAttributeId == pva.Id);

                switch (pva.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.Boxes:
                    {
                        var valueId = selectedItems.FirstOrDefault()
                            ?.Value
                            ?.SplitSafe(',')
                            ?.FirstOrDefault()
                            ?.ToInt() ?? 0;

                        if (valueId > 0)
                        {
                            selection.AddAttributeValue(pva.Id, valueId);
                        }
                    }
                    break;

                    case AttributeControlType.Checkboxes:
                        foreach (var item in selectedItems)
                        {
                            var valueId = item.Value.SplitSafe(',').FirstOrDefault()?.ToInt() ?? 0;
                            if (valueId > 0)
                            {
                                selection.AddAttributeValue(pva.Id, valueId);
                            }
                        }
                        break;

                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                    {
                        var value = string.Join(",", selectedItems.Select(x => x.Value));
                        if (value.HasValue())
                        {
                            selection.AddAttributeValue(pva.Id, value);
                        }
                    }
                    break;

                    case AttributeControlType.Datepicker:
                        var firstItemDate = selectedItems.FirstOrDefault()?.Date;
                        if (firstItemDate.HasValue)
                        {
                            selection.AddAttributeValue(pva.Id, firstItemDate.Value.ToString("D"));
                        }
                        break;

                    case AttributeControlType.FileUpload:
                        if (getFilesFromRequest)
                        {
                            var request = _httpContextAccessor?.HttpContext?.Request;
                            if (request != null && request.HasFormContentType)
                            {
                                var files = request.Form.Files;
                                if (files != null && files.Count > 0)
                                {
                                    var postedFile = files[ProductVariantQueryItem.CreateKey(productId, bundleItemId, pva.ProductAttributeId, pva.Id)];
                                    if (postedFile != null && postedFile.FileName.HasValue())
                                    {
                                        if (postedFile.Length > _catalogSettings.Value.FileUploadMaximumSizeBytes)
                                        {
                                            warnings.Add(T("ShoppingCart.MaximumUploadedFileSize", (int)(_catalogSettings.Value.FileUploadMaximumSizeBytes / 1024)));
                                        }
                                        else
                                        {
                                            var download = new Download
                                            {
                                                DownloadGuid = Guid.NewGuid(),
                                                UseDownloadUrl = false,
                                                DownloadUrl = string.Empty,
                                                UpdatedOnUtc = DateTime.UtcNow,
                                                EntityId = productId,
                                                EntityName = "ProductAttribute"
                                            };

                                            using var stream = postedFile.OpenReadStream();
                                            await _downloadService.Value.InsertDownloadAsync(download, stream, postedFile.FileName);

                                            selection.AddAttributeValue(pva.Id, download.DownloadGuid.ToString());
                                        }
                                    }
                                }
                            }
                        }
                        else if (Guid.TryParse(selectedItems.FirstOrDefault()?.Value, out var downloadGuid) && downloadGuid != Guid.Empty)
                        {
                            var download = await _db.Downloads.Where(x => x.DownloadGuid == downloadGuid).FirstOrDefaultAsync();
                            if (download != null)
                            {
                                if (download.IsTransient)
                                {
                                    download.IsTransient = false;
                                    await _db.SaveChangesAsync();
                                }

                                selection.AddAttributeValue(pva.Id, download.DownloadGuid.ToString());
                            }
                        }
                        break;
                }
            }

            return (selection, warnings);
        }

        public virtual void ClearCachedAttributes()
        {
            _requestCache.RemoveByPattern(ATTRIBUTES_PATTERN_KEY);
            _requestCache.RemoveByPattern(ATTRIBUTEVALUES_PATTERN_KEY);
        }

        public virtual async Task<ProductVariantAttributeCombination> FindAttributeCombinationAsync(int productId, ProductVariantAttributeSelection selection)
        {
            if (productId == 0 || !(selection?.AttributesMap?.Any() ?? false))
            {
                return null;
            }

            var cacheKey = ATTRIBUTECOMBINATION_BY_IDJSON_KEY.FormatInvariant(productId, selection.AsJson());

            var combination = await _requestCache.GetAsync(cacheKey, async () =>
            {
                var combinations = await _db.ProductVariantAttributeCombinations
                    .AsNoTracking()
                    .Where(x => x.ProductId == productId)
                    .Select(x => new ProductVariantAttributeCombination
                    {
                        Id = x.Id,
                        RawAttributes = x.RawAttributes
                    })
                    .ToListAsync();

                return await FindCombinationByAttributeSelection(selection, combinations);
            });

            return combination;
        }

        public virtual async Task<ProductVariantAttributeCombination> MergeWithCombinationAsync(
            Product product,
            ProductVariantAttributeSelection selection,
            ProductVariantAttributeCombination combination = null)
        {
            Guard.NotNull(product, nameof(product));

            combination ??= await FindAttributeCombinationAsync(product.Id, selection);

            if (combination != null && combination.IsActive)
            {
                product.MergeWithCombination(combination);
            }
            else if (product.MergedDataValues != null)
            {
                product.MergedDataValues.Clear();
            }

            return combination;
        }

        public virtual async Task<int> MergeWithCombinationAsync(IEnumerable<ShoppingCartItem> cartItems)
        {
            Guard.NotNull(cartItems, nameof(cartItems));

            var productIds = cartItems
                .Select(x => x.ProductId)
                .Distinct()
                .ToArray();

            if (!productIds.Any())
            {
                return 0;
            }

            var allCombinations = await _db.ProductVariantAttributeCombinations
                .AsNoTracking()
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => new ProductVariantAttributeCombination
                {
                    Id = x.Id,
                    ProductId = x.ProductId,
                    RawAttributes = x.RawAttributes
                })
                .ToListAsync();

            var num = 0;
            var combinationsMap = allCombinations.ToMultimap(x => x.ProductId, x => x);

            foreach (var cartItem in cartItems)
            {
                if (cartItem.AttributeSelection.AttributesMap.Any() &&
                    combinationsMap.TryGetValues(cartItem.ProductId, out var combinationsForProduct))
                {
                    var combination = await FindCombinationByAttributeSelection(cartItem.AttributeSelection, combinationsForProduct);
                    if (combination != null)
                    {
                        await MergeWithCombinationAsync(cartItem.Product, cartItem.AttributeSelection, combination);
                        ++num;
                    }
                }
            }

            return num;
        }

        public virtual async Task<CombinationAvailabilityInfo> IsCombinationAvailableAsync(
            Product product,
            IEnumerable<ProductVariantAttribute> attributes,
            IEnumerable<ProductVariantAttributeValue> selectedValues,
            ProductVariantAttributeValue currentValue)
        {
            if (product == null ||
                _performanceSettings.MaxUnavailableAttributeCombinations <= 0 ||
                !(selectedValues?.Any() ?? false))
            {
                return null;
            }

            // Get unavailable combinations.
            var unavailableCombinations = await _cache.GetAsync(UNAVAILABLE_COMBINATIONS_KEY.FormatInvariant(product.Id), async o =>
            {
                o.ExpiresIn(TimeSpan.FromMinutes(10));

                var data = new Dictionary<string, CombinationAvailabilityInfo>();
                var query = _db.ProductVariantAttributeCombinations
                    .AsNoTracking()
                    .Where(x => x.ProductId == product.Id);

                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
                {
                    query = query.Where(x => !x.IsActive || (x.StockQuantity <= 0 && !x.AllowOutOfStockOrders));
                }
                else
                {
                    query = query.Where(x => !x.IsActive);
                }

                // Do not proceed if there are too many unavailable combinations.
                var unavailableCombinationsCount = await query.CountAsync();

                if (unavailableCombinationsCount <= _performanceSettings.MaxUnavailableAttributeCombinations)
                {
                    var pager = query.ToFastPager();

                    while ((await pager.ReadNextPageAsync<ProductVariantAttributeCombination>()).Out(out var combinations))
                    {
                        foreach (var combination in combinations)
                        {
                            if (combination.AttributeSelection.AttributesMap.Any())
                            {
                                // <ProductVariantAttribute.Id>:<ProductVariantAttributeValue.Id>[,...]
                                var valuesKeys = combination.AttributeSelection.AttributesMap
                                    .OrderBy(x => x.Key)
                                    .Select(x => $"{x.Key}:{string.Join(",", x.Value.OrderBy(y => y))}");

                                data[string.Join("-", valuesKeys)] = new CombinationAvailabilityInfo
                                {
                                    IsActive = combination.IsActive,
                                    IsOutOfStock = combination.StockQuantity <= 0 && !combination.AllowOutOfStockOrders
                                };
                            }
                        }
                    }
                }

                return data;
            });

            if (!unavailableCombinations.Any())
            {
                return null;
            }

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            var selectedValuesMap = selectedValues.ToMultimap(x => x.ProductVariantAttributeId, x => x);

            if (attributes == null || currentValue == null)
            {
                // Create key to test selectedValues.
                foreach (var kvp in selectedValuesMap.OrderBy(x => x.Key))
                {
                    Append(sb, kvp.Key, kvp.Value.Select(x => x.Id).Distinct());
                }
            }
            else
            {
                // Create key to test currentValue.
                foreach (var attribute in attributes.OrderBy(x => x.Id))
                {
                    IEnumerable<int> valueIds;

                    var selectedIds = selectedValuesMap.ContainsKey(attribute.Id)
                        ? selectedValuesMap[attribute.Id].Select(x => x.Id)
                        : null;

                    if (attribute.Id == currentValue.ProductVariantAttributeId)
                    {
                        // Attribute to be tested.
                        if (selectedIds != null && attribute.IsMultipleChoice)
                        {
                            // Take selected values and append current value.
                            valueIds = selectedIds.Append(currentValue.Id).Distinct();
                        }
                        else
                        {
                            // Single selection attribute -> take current value.
                            valueIds = new[] { currentValue.Id };
                        }
                    }
                    else
                    {
                        // Other attribute.
                        if (selectedIds != null)
                        {
                            // Take selected value(s).
                            valueIds = selectedIds;
                        }
                        else
                        {
                            // No selected value -> no unavailable combination.
                            return null;
                        }
                    }

                    Append(sb, attribute.Id, valueIds);
                }
            }

            var key = sb.ToString();
            //$"{!unavailableCombinations.ContainsKey(key),-5} {currentValue.ProductVariantAttributeId}:{currentValue.Id} -> {key}".Dump();

            if (unavailableCombinations.TryGetValue(key, out var availability))
            {
                return availability;
            }

            return null;

            static void Append(StringBuilder sb, int pvaId, IEnumerable<int> pvavIds)
            {
                var idsStr = string.Join(",", pvavIds.OrderBy(x => x));

                if (sb.Length > 0)
                {
                    sb.Append('-');
                }
                sb.Append($"{pvaId}:{idsStr}");
            }
        }

        protected virtual async Task<List<ProductVariantAttributeValue>> LoadAttributeValuesAsync(int[] attributeIds, int[] valueIds)
        {
            if (!attributeIds.Any() || !valueIds.Any())
            {
                return new List<ProductVariantAttributeValue>();
            }

            // ProductVariantAttributeSelection can also contain numeric values of text fields that are not ProductVariantAttributeValue IDs!
            // That is why it is important to also filter by list types because only list types (e.g. dropdown list)
            // can have assigned ProductVariantAttributeValue entities.

            return await _db.ProductVariantAttributeValues
                .Include(x => x.ProductVariantAttribute)
                .ThenInclude(x => x.ProductAttribute)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(x => attributeIds.Contains(x.ProductVariantAttributeId) && valueIds.Contains(x.Id))
                .ApplyListTypeFilter()
                .ToListAsync();
        }

        private async Task<ProductVariantAttributeCombination> FindCombinationByAttributeSelection(
            ProductVariantAttributeSelection selection,
            ICollection<ProductVariantAttributeCombination> attributeCombinationsLookup)
        {
            if (!attributeCombinationsLookup.Any())
            {
                return null;
            }

            ProductVariantAttributeSelection listTypeSelection;
            var listTypeValues = await MaterializeProductVariantAttributeValuesAsync(selection);
            var listTypeAttributesIds = listTypeValues.Select(x => x.ProductVariantAttributeId).ToArray();

            if (selection.AttributesMap.Any(x => !listTypeAttributesIds.Contains(x.Key)))
            {
                // Remove attributes that are not of type list from selection.
                listTypeSelection = new ProductVariantAttributeSelection(null);

                foreach (var item in selection.AttributesMap.Where(x => listTypeAttributesIds.Contains(x.Key)))
                {
                    listTypeSelection.AddAttribute(item.Key, item.Value);
                }
            }
            else
            {
                listTypeSelection = selection;
            }

            // TODO: (core) (important) (future) Save combination hash in table and always lookup by hash instead of iterating thru local data to find a match.
            foreach (var combination in attributeCombinationsLookup)
            {
                if (listTypeSelection.Equals(combination.AttributeSelection))
                {
                    return await _db.ProductVariantAttributeCombinations.FindByIdAsync(combination.Id);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all integer values of an attribute selection that are not 0.
        /// Usually these are <see cref="BaseEntity.Id"/> but can 
        /// (depending on <see cref="AttributeControlType"/>) also be other numeric values of text fields.
        /// </summary>
        private static int[] GetIntegerValues(ProductVariantAttributeSelection selection)
        {
            return selection.AttributesMap
                .SelectMany(x => x.Value)
                .Select(x => x.ToString())
                .Where(x => x.HasValue())
                .Select(x => x.ToInt())
                .Where(x => x != 0)
                .Distinct()
                .ToArray();
        }

        class AttributeSelectionInfo
        {
            public AttributeSelectionInfo(ProductVariantAttributeSelection selection, string valuesCacheKey)
            {
                Selection = selection;
                ValuesCacheKey = valuesCacheKey;
                AttributeIds = selection.AttributesMap.Select(x => x.Key).ToArray();
                Values = GetIntegerValues(selection);
            }

            public string ValuesCacheKey { get; }
            public ProductVariantAttributeSelection Selection { get; }
            public int[] AttributeIds { get; }
            public int[] Values { get; }
        }
    }
}
