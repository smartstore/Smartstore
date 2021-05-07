using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Serilog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal class DataExporterContext
    {
        public DataExporterContext(DataExportRequest request, bool isPreview, CancellationToken cancellationToken)
        {
            Request = request;
            IsPreview = isPreview;
            CancellationToken = cancellationToken;

            FolderContent = request.Profile.GetExportDirectory(true, true);

            Filter = Deserialize<ExportFilter>(request.Profile.Filtering) ?? new();
            Projection = Deserialize<ExportProjection>(request.Profile.Projection) ?? new();

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
        public Dictionary<int, RecordStats> StatsPerStore { get; set; } = new();

        public ExportFilter Filter { get; private set; }
        public ExportProjection Projection { get; private set; }
        public Currency ContextCurrency { get; set; }
        public Customer ContextCustomer { get; set; }
        public Language ContextLanguage { get; set; }
        public int LanguageId => Projection.LanguageId ?? 0;
        public Store Store { get; set; }

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

        private static T Deserialize<T>(string xml)
        {
            // TODO: (mg) (core) XmlHelper should be ported after all. It is called from numerous places.
            try
            {
                if (xml.HasValue())
                {
                    using var reader = new StringReader(xml);
                    var serializer = new XmlSerializer(typeof(T));
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch 
            { 
            }

            return default;
        }
    }

    internal class RecordStats
    {
        public int TotalRecords { get; set; }
        public int MaxId { get; set; }
    }
}
