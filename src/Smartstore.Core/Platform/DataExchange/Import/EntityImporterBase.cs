using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Autofac;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.IO;
using Smartstore.Net;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Import
{
    public abstract partial class EntityImporterBase : IEntityImporter
    {
        // Maps (per batch) already downloaded URLs to file names.
        // Background: in some imports subsequent products (e.g. associated products)
        // share the same images, where multiple downloading is unnecessary.
        private readonly Dictionary<string, string> _downloadedItems = new();

        protected ICommonServices _services;
        protected SmartDbContext _db;
        protected ILocalizedEntityService _localizedEntityService;
        protected IStoreMappingService _storeMappingService;
        protected IUrlService _urlService;

        protected EntityImporterBase(
            ICommonServices services,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            IUrlService urlService)
        {
            _services = services;
            _db = services.DbContext;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _urlService = urlService;

            // Always turn image post-processing off during imports. It can heavily decrease processing time.
            _services.MediaService.ImagePostProcessingEnabled = false;
        }

        /// <inheritdoc/>
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

        protected virtual async Task<int> ProcessLocalizationsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            IDictionary<string, Expression<Func<TEntity, string>>> localizableProperties)
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
            var collection = await _localizedEntityService.GetLocalizedPropertyCollectionAsync(keyGroup, entityIds);

            foreach (var row in batch)
            {
                foreach (var prop in localizedProps)
                {
                    var keySelector = localizableProperties[prop];
                    foreach (var language in context.Languages)
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
                return await _db.SaveChangesAsync(context.CancelToken);
            }

            return 0;
        }

        protected virtual async Task<int> ProcessStoreMappingsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch)
            where TEntity : BaseEntity, IStoreRestricted
        {
            var shouldSave = false;
            var entityIds = batch.Select(x => x.Entity.Id).ToArray();
            if (!entityIds.Any())
            {
                return 0;
            }

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

                    foreach (var store in context.Stores)
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
                return await _db.SaveChangesAsync(context.CancelToken);
            }

            return 0;
        }

        protected virtual async Task<int> ProcessSlugsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            string entityName)
            where TEntity : BaseEntity, ISlugSupported
        {
            using var scope = _urlService.CreateBatchScope(_db);

            // TODO: (core) (perf) IUrlService.ValidateSlugAsync ignores prefetched data.

            foreach (var row in batch)
            {
                try
                {
                    if (row.TryGetDataValue("SeName", out string seName) || row.IsNew || row.NameChanged)
                    {
                        scope.ApplySlugs(await _urlService.ValidateSlugAsync(row.Entity, seName, true));

                        // Process localized slugs.
                        foreach (var language in context.Languages)
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
            return await scope.CommitAsync(context.CancelToken);
        }

        protected virtual DownloadManagerItem CreateDownloadItem(ImportExecuteContext context, string urlOrPath, int displayOrder)
        {
            if (urlOrPath.IsEmpty())
            {
                return null;
            }

            try
            {
                var item = new DownloadManagerItem
                {
                    Id = displayOrder,
                    DisplayOrder = displayOrder
                };

                if (urlOrPath.IsWebUrl())
                {
                    // We append quality to avoid importing of image duplicates.
                    item.Url = _services.WebHelper.ModifyQueryString(urlOrPath, "q=100", null);

                    if (_downloadedItems.ContainsKey(urlOrPath))
                    {
                        // URL has already been downloaded.
                        item.Success = true;
                        item.FileName = _downloadedItems[urlOrPath];
                    }
                    else
                    {
                        item.FileName = DownloadManager.GetFileNameFromUrl(urlOrPath) ?? Path.GetRandomFileName();
                    }

                    item.Path = GetAbsolutePath(context.ImageDownloadDirectory, item.FileName);
                }
                else
                {
                    item.Success = true;
                    item.FileName = Path.GetFileName(urlOrPath).ToValidFileName().NullEmpty() ?? Path.GetRandomFileName();

                    item.Path = Path.IsPathRooted(urlOrPath)
                        ? urlOrPath
                        : GetAbsolutePath(context.ImageDirectory, urlOrPath);
                }

                item.MimeType = MimeTypes.MapNameToMimeType(item.FileName);

                return item;
            }
            catch
            {
                context.Result.AddWarning($"Failed to prepare image download for '{urlOrPath.NaIfEmpty()}'. Skipping file.");
                return null;
            }

            static string GetAbsolutePath(IDirectory directory, string fileNameOrRelativePath)
            {
                return directory.FileSystem.PathCombine(directory.PhysicalPath, fileNameOrRelativePath).Replace('/', '\\');
            }
        }

        protected virtual void CacheDownloadItem(ImportExecuteContext context, DownloadManagerItem item)
        {
            if (item.Success && item.Url.HasValue() && !_downloadedItems.ContainsKey(item.Url))
            {
                _downloadedItems[item.Url] = Path.GetFileName(item.Path);
            }
        }

        protected static int? ZeroToNull(object value, CultureInfo culture)
        {
            if (CommonHelper.TryConvert(value, culture, out int result) && result > 0)
            {
                return result;
            }

            return null;
        }
    }
}
