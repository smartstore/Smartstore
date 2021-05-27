using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Import
{
    public abstract class EntityImporterBase : IEntityImporter
    {
        protected SmartDbContext _db;
        protected IStoreContext _storeContext;
        protected ILanguageService _languageService;
        protected ILocalizedEntityService _localizedEntityService;
        protected IStoreMappingService _storeMappingService;
        protected IUrlService _urlService;
        protected IMediaService _mediaService;

        protected EntityImporterBase(
            SmartDbContext db,
            IStoreContext storeContext,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            IUrlService urlService,
            IMediaService mediaService)
        {
            _db = db;
            _storeContext = storeContext;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _urlService = urlService;
            _mediaService = mediaService;

            // Always turn image post-processing off during imports. It can heavily decrease processing time.
            _mediaService.ImagePostProcessingEnabled = false;
        }

        public DateTime UtcNow { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// URL to file name map. To avoid downloading image several times.
        /// </summary>
		public Dictionary<string, string> DownloadedItems { get; private set; } = new();

        public IDirectory ImageDownloadFolder { get; private set; }

        public IDirectory ImageFolder { get; private set; }

        public Task ExecuteAsync(ImportExecuteContext context, CancellationToken cancelToken = default)
        {
            return ProcessBatchAsync(context, cancelToken);
        }

        /// <summary>
        /// Imports a batch of data into the database.
        /// </summary>
        /// <param name="context">Import execution context.</param>
        /// <param name="cancelToken">A cancellation token to cancel the import.</param>
        protected abstract Task ProcessBatchAsync(ImportExecuteContext context, CancellationToken cancelToken = default);

        protected void InitializeBatch(ILifetimeScope scope, ImportExecuteContext context)
        {
            Guard.NotNull(scope, nameof(scope));
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.ImportDirectory, nameof(context.ImportDirectory));

            // TODO: (mg) (core) Find a way to re-resolve the IEntityImporter (ROOT) instance from new lifetime scope per batch
            // (instead of resolving all inner dependencies here). Keep in mind that IEntityImporter impls can contain other custom
            // ctor dependencies, these also must come from new scope.
            // This inevitably means: IEntityImporter.ImportAsync() has to be called ONCE PER BATCH from outside (DataImporter), hence
            // passing data segmentation control over to DataImporter. For better API design ImportAsync() method here could be renamed to ProcessBatch().
            // TBD with MC.

            //_db = scope.Resolve<SmartDbContext>();
            //_storeContext = scope.Resolve<IStoreContext>();
            //_languageService = scope.Resolve<ILanguageService>();
            //_localizedEntityService = scope.Resolve<ILocalizedEntityService>();
            //_storeMappingService = scope.Resolve<IStoreMappingService>();
            //_urlService = scope.Resolve<IUrlService>();

            //context.Result.TotalRecords = context.DataSegmenter.TotalRows;
        }

        protected virtual async Task<int> ProcessLocalizationsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            IDictionary<string, Expression<Func<TEntity, string>>> localizableProperties,
            CancellationToken cancelToken = default) 
            where TEntity : BaseEntity, ILocalizedEntity
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(batch, nameof(batch));
            Guard.NotNull(localizableProperties, nameof(localizableProperties));

            var entityIds = batch.Select(x => x.Entity.Id).ToArray();
            if (!entityIds.Any())
            {
                return 0;
            }

            // Perf: determine whether our localizable properties actually have 
            // counterparts in the source BEFORE import batch begins. This way we spare ourself
            // to query over and over for values.
            var localizedProps = (from kvp in localizableProperties
                                  where context.DataSegmenter.GetColumnIndexes(kvp.Key).Length > 0
                                  select kvp.Key).ToArray();
            if (!localizedProps.Any())
            {
                return 0;
            }

            var shouldSave = false;
            var keyGroup = nameof(TEntity);
            var languages = await _languageService.GetAllLanguagesAsync(true);
            var collection = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(keyGroup, entityIds);

            foreach (var row in batch)
            {
                foreach (var prop in localizedProps)
                {
                    var keySelector = localizableProperties[prop];
                    foreach (var language in languages)
                    {
                        if (row.TryGetDataValue(prop /* ColumnName */, language.UniqueSeoCode, out string value))
                        {
                            var localizedProperty = collection.Find(language.Id, row.Entity.Id, keyGroup);
                            if (localizedProperty != null)
                            {
                                if (string.IsNullOrEmpty(value))
                                {
                                    // Delete.
                                    _db.LocalizedProperties.Remove(localizedProperty);
                                    shouldSave = true;
                                }
                                else if (localizedProperty.LocaleValue != value)
                                {
                                    // Update.
                                    localizedProperty.LocaleValue = value;
                                    shouldSave = true;
                                }
                            }
                            else if (!string.IsNullOrEmpty(value))
                            {
                                // Insert.
                                var propInfo = keySelector.ExtractPropertyInfo();
                                if (propInfo == null)
                                {
                                    throw new ArgumentException($"Expression '{keySelector}' does not refer to a property.");
                                }

                                _db.LocalizedProperties.Add(new LocalizedProperty
                                {
                                    EntityId = row.Entity.Id,
                                    LanguageId = language.Id,
                                    LocaleKey = propInfo.Name,
                                    LocaleKeyGroup = keyGroup,
                                    LocaleValue = value
                                });
                                shouldSave = true;
                            }
                        }
                    }
                }
            }

            if (shouldSave)
            {
                // Commit whole batch at once.
                return await _db.SaveChangesAsync(cancelToken);
            }

            return 0;
        }

        protected virtual async Task<int> ProcessStoreMappingsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            CancellationToken cancelToken = default) 
            where TEntity : BaseEntity, IStoreRestricted
        {
            var shouldSave = false;
            var entityIds = batch.Select(x => x.Entity.Id).ToArray();
            if (!entityIds.Any())
            {
                return 0;
            }

            var stores = _storeContext.GetAllStores();
            var collection = await _storeMappingService.GetStoreMappingCollectionAsync(nameof(TEntity), entityIds);

            foreach (var row in batch)
            {
                var storeIds = row.GetDataValue<List<int>>("StoreIds");
                var hasStoreIds = storeIds?.Any() ?? false;
                
                if (storeIds.Count == 1 && storeIds[0] == 0)
                {
                    hasStoreIds = false;
                }

                if (hasStoreIds)
                {
                    row.Entity.LimitedToStores = true;

                    foreach (var store in stores)
                    {
                        if (storeIds.Contains(store.Id))
                        {
                            // Add the mapping, if missing.
                            if (collection.Find(row.Entity.Id, store.Id) == null)
                            {
                                _storeMappingService.AddStoreMapping(row.Entity, store.Id);
                                shouldSave = true;
                            }
                        }
                        else
                        {
                            // Delete the mapping, if it exists.
                            var storeMappingToDelete = collection.Find(row.Entity.Id, store.Id);
                            if (storeMappingToDelete != null)
                            {
                                _db.StoreMappings.Remove(storeMappingToDelete);
                                shouldSave = true;
                            }
                        }
                    }
                }
                else if (row.Entity.LimitedToStores)
                {
                    row.Entity.LimitedToStores = false;
                    shouldSave = true;
                }
            }

            if (shouldSave)
            {
                // Commit whole batch at once.
                return await _db.SaveChangesAsync(cancelToken);
            }

            return 0;
        }

        protected virtual async Task<int> ProcessSlugsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            string entityName,
            CancellationToken cancelToken = default)
            where TEntity : BaseEntity, ISlugSupported
        {
            var languages = await _languageService.GetAllLanguagesAsync(true);

            using var scope = _urlService.CreateBatchScope(_db);

            // TODO: (mg) (core) (perf) Prefetching is missing. Without prefetching, _urlService.ValidateSlugAsync()
            // makes one DB call per invoke.

            foreach (var row in batch)
            {
                try
                {
                    if (row.TryGetDataValue("SeName", out string seName) || row.IsNew || row.NameChanged)
                    {
                        scope.ApplySlugs(await _urlService.ValidateSlugAsync(row.Entity, seName, true));

                        // Process localized slugs.
                        foreach (var language in languages)
                        {
                            var hasSeName = row.TryGetDataValue("SeName", language.UniqueSeoCode, out seName);
                            var hasLocalizedName = row.TryGetDataValue("Name", language.UniqueSeoCode, out string localizedName);

                            if (hasSeName || hasLocalizedName)
                            {
                                // ValidateSlugAsync has no 'name' parameter anymore.
                                // We ourselves must ensure that 'Name[<UniqueSeoCode>]' is taken into account.
                                if (string.IsNullOrWhiteSpace(seName))
                                {
                                    seName = localizedName;
                                }

                                scope.ApplySlugs(await _urlService.ValidateSlugAsync(row.Entity, seName, false, language.Id));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result.AddWarning(ex.Message, row.RowInfo, "SeName");
                }
            }

            // Commit whole batch at once.
            return await scope.CommitAsync(cancelToken);
        }
    }
}
