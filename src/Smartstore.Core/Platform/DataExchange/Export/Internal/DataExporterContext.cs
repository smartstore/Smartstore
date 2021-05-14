using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal class DataExporterContext
    {
        private readonly static string[] _globalTranslationEntities = new[] 
        {
            nameof(Currency),
            nameof(Country),
            nameof(StateProvince),
            nameof(DeliveryTime),
            nameof(QuantityUnit),
            nameof(Manufacturer),
            nameof(Category),
        };

        private readonly static string[] _globalSlugEntities = new[]
        {
            nameof(Category),
            nameof(Manufacturer)
        };

        public DataExporterContext(DataExportRequest request, bool isPreview, CancellationToken cancellationToken)
        {
            Request = request;
            IsPreview = isPreview;
            CancellationToken = cancellationToken;

            FolderContent = request.Profile.GetExportDirectory(true, true);

            Filter = XmlHelper.Deserialize<ExportFilter>(request.Profile.Filtering);
            Projection = XmlHelper.Deserialize<ExportProjection>(request.Profile.Projection);

            if (request.Profile.Projection.IsEmpty())
            {
                Projection.DescriptionMergingId = (int)ExportDescriptionMerging.Description;
            }

            Result = new DataExportResult
            {
                FileFolder = IsFileBasedExport ? FolderContent : null
            };

            ExecuteContext = new ExportExecuteContext(Result, FolderContent, CancellationToken)
            {
                Filter = Filter,
                Projection = Projection,
                ProfileId = request.Profile.Id
            };

            if (!IsPreview)
            {
                ExecuteContext.ProgressValueSetter = Request.ProgressValueSetter;
            }
        }

        public DataExportRequest Request { get; private set; }
        public bool IsPreview { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public ILogger Log { get; set; }
        public ExportExecuteContext ExecuteContext { get; set; }
        public DataExportResult Result { get; set; }
        public PriceCalculationOptions PriceCalculationOptions { get; set; }

        /// <summary>
        /// All entity identifiers per export.
        /// </summary>
        public List<int> EntityIdsLoaded { get; set; } = new();

        /// <summary>
        /// All entity identifiers per segment (used to avoid exporting products multiple times).
        /// </summary>
        public HashSet<int> EntityIdsPerSegment { get; set; } = new();
        public int LastId { get; set; }

        public string ProgressInfo { get; set; }
        public int RecordCount { get; set; }
        public Dictionary<int, ShopMetadata> ShopMetadata { get; set; } = new();

        public ExportFilter Filter { get; private set; }
        public ExportProjection Projection { get; private set; }
        public Store Store { get; set; }
        public Currency ContextCurrency { get; set; }
        public Customer ContextCustomer { get; set; }
        public Language ContextLanguage { get; set; }
        public int LanguageId => Projection.LanguageId ?? 0;
        public int MasterLanguageId { get; set; }

        public string FolderContent { get; private set; }

        public bool IsFileBasedExport 
            => Request.Provider == null || Request.Provider.Value == null || Request.Provider.Value.FileExtension.HasValue();

        #region  Data loaded once per export

        public Dictionary<int, DeliveryTime> DeliveryTimes { get; set; } = new();
        public Dictionary<int, QuantityUnit> QuantityUnits { get; set; } = new();
        public Dictionary<int, Store> Stores { get; set; } = new();
        public Dictionary<int, Language> Languages { get; set; } = new();
        public Dictionary<int, Country> Countries { get; set; } = new();
        public Dictionary<int, string> ProductTemplates { get; set; } = new();
        public Dictionary<int, string> CategoryTemplates { get; set; } = new();
        public HashSet<string> NewsletterSubscriptions { get; set; } = new();

        /// <summary>
        /// All translations for global scopes (like Category, Manufacturer etc.)
        /// </summary>
        public Dictionary<string, LocalizedPropertyCollection> Translations { get; set; } = new();
        public Dictionary<string, UrlRecordCollection> UrlRecords { get; set; } = new();

        #endregion

        #region Data loaded once per page

        public ProductBatchContext ProductBatchContext { get; set; }
        public ProductBatchContext AssociatedProductBatchContext { get; set; }
        public OrderBatchContext OrderBatchContext { get; set; }
        public ManufacturerBatchContext ManufacturerBatchContext { get; set; }
        public CategoryBatchContext CategoryBatchContext { get; set; }
        public CustomerBatchContext CustomerBatchContext { get; set; }

        /// <summary>
        /// All per page translations (like ProductVariantAttributeValue etc.)
        /// </summary>
        public Dictionary<string, LocalizedPropertyCollection> TranslationsPerPage { get; set; } = new();
        public Dictionary<string, UrlRecordCollection> UrlRecordsPerPage { get; set; } = new();

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
