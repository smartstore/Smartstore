using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal class DataExporterContext
    {
        private readonly static string[] _globalTranslationEntities =
        [
            nameof(Currency),
            nameof(Country),
            nameof(StateProvince),
            nameof(DeliveryTime),
            nameof(QuantityUnit),
            nameof(Manufacturer),
            nameof(Category),
        ];

        private readonly static string[] _globalSlugEntities =
        [
            nameof(Category),
            nameof(Manufacturer)
        ];

        public bool IsPreview { get; init; }
        public DataExportRequest Request { get; init; }
        public IDirectory ExportDirectory { get; set; }
        public CancellationToken CancelToken { get; init; }
        public IFile ZipFile { get; set; }
        public ILogger Log { get; set; }
        public ExportExecuteContext ExecuteContext { get; set; }
        public DataExportResult Result { get; set; } = new();
        public PriceCalculationOptions PriceCalculationOptions { get; set; }
        public PriceCalculationOptions AttributeCombinationPriceCalcOptions { get; set; }

        /// <summary>
        /// All entity identifiers per export.
        /// </summary>
        public List<int> EntityIdsLoaded { get; set; } = [];

        /// <summary>
        /// All entity identifiers per segment (used to avoid exporting products multiple times).
        /// </summary>
        public HashSet<int> EntityIdsPerSegment { get; set; } = [];
        public int LastId { get; set; }

        public string ProgressInfo { get; set; }
        public int RecordCount { get; set; }
        public Dictionary<int, ShopMetadata> ShopMetadata { get; set; } = [];

        public ExportFilter Filter { get; init; }
        public Store Store { get; set; }
        public ExportProjection Projection { get; init; }
        public int LanguageId => Projection.LanguageId ?? 0;

        public bool IsFileBasedExport
            => Request?.Provider?.Value?.FileExtension?.HasValue() ?? false;

        #region Data loaded once per export

        public Dictionary<int, DeliveryTime> DeliveryTimes { get; set; } = [];
        public Dictionary<int, QuantityUnit> QuantityUnits { get; set; } = [];
        public Dictionary<int, PriceLabel> PriceLabels { get; set; } = [];
        public Dictionary<int, Store> Stores { get; set; } = [];
        public Dictionary<int, Language> Languages { get; set; } = [];
        public Dictionary<int, Country> Countries { get; set; } = [];
        public Dictionary<int, StateProvince> StateProvinces { get; set; } = [];
        public Dictionary<int, string> ProductTemplates { get; set; } = [];
        public Dictionary<int, string> CategoryTemplates { get; set; } = [];
        public Dictionary<int, string> ManufacturerTemplates { get; set; } = [];
        public HashSet<string> NewsletterSubscriptions { get; set; } = [];

        /// <summary>
        /// All translations for global scopes (like Category, Manufacturer etc.)
        /// </summary>
        public Dictionary<string, LocalizedPropertyCollection> Translations { get; set; } = [];
        public Dictionary<string, UrlRecordCollection> UrlRecords { get; set; } = [];

        #endregion

        #region Data loaded once per page

        /// <summary>
        /// Associated product data with applied filters (e.g. for store and customer).
        /// </summary>
        public ProductBatchContext ProductBatchContext { get; set; }

        /// <summary>
        /// Associated product data without applied filters.
        /// </summary>
        /// <remarks>
        /// Required for tier prices (and maybe others in the future) where we need both: unfiltered data for export and filtered data for pricing.
        /// </remarks>
        public ProductBatchContext ProductBatchContextWithoutFilters { get; set; }

        public ProductBatchContext AssociatedProductBatchContext { get; set; }
        public OrderBatchContext OrderBatchContext { get; set; }
        public ManufacturerBatchContext ManufacturerBatchContext { get; set; }
        public CategoryBatchContext CategoryBatchContext { get; set; }
        public CustomerBatchContext CustomerBatchContext { get; set; }

        /// <summary>
        /// All per page translations (like ProductVariantAttributeValue etc.)
        /// </summary>
        public Dictionary<string, LocalizedPropertyCollection> TranslationsPerPage { get; set; } = [];
        public Dictionary<string, UrlRecordCollection> UrlRecordsPerPage { get; set; } = [];

        #endregion

        #region Utilities

        public void SetLoadedEntityIds(IEnumerable<int> ids)
        {
            EntityIdsLoaded = EntityIdsLoaded
                .Union(ids)
                .Distinct()
                .ToList();
        }

        public bool Supports(ExportFeatures feature)
        {
            return !IsPreview && Request.Provider.Metadata.ExportFeatures.HasFlag(feature);
        }

        public LocalizedPropertyCollection GetTranslations<TEntity>()
            where TEntity : BaseEntity
        {
            var entityName = typeof(TEntity).Name;

            if (_globalTranslationEntities.Contains(entityName))
            {
                return Translations.GetValueOrDefault(entityName);
            }

            return TranslationsPerPage.GetValueOrDefault(entityName);
        }

        public string GetTranslation<TEntity>(TEntity entity, string localeKey, string defaultValue = default)
            where TEntity : BaseEntity
        {
            return GetTranslations<TEntity>()?.GetValue(LanguageId, entity.Id, localeKey) ?? defaultValue;
        }

        public UrlRecordCollection GetUrlRecords<TEntity>()
            where TEntity : BaseEntity
        {
            var entityName = typeof(TEntity).Name;

            if (_globalSlugEntities.Contains(entityName))
            {
                return UrlRecords.GetValueOrDefault(entityName);
            }

            return UrlRecordsPerPage.GetValueOrDefault(entityName);
        }

        public string GetUrlRecord<TEntity>(TEntity entity, bool returnDefaultValue = true)
            where TEntity : BaseEntity
        {
            return GetUrlRecords<TEntity>()?.GetSlug(LanguageId, entity.Id, returnDefaultValue);
        }

        #endregion
    }

    internal class ShopMetadata
    {
        public int MasterLanguageId { get; set; }
        public int TotalRecords { get; set; }
        public int MaxId { get; set; }
    }
}
