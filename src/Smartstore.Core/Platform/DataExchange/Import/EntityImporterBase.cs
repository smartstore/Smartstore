using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Domain;
using Smartstore.Http;
using Smartstore.IO;
using Smartstore.Net.Http;
using Smartstore.Utilities;

namespace Smartstore.Core.DataExchange.Import
{
    public abstract partial class EntityImporterBase : IEntityImporter
    {
        // Maps (per batch) already downloaded URLs to file names.
        // Background: in some imports subsequent products (e.g. associated products)
        // share the same images, where multiple downloading is unnecessary.
        private readonly Dictionary<string, string> _downloadedItems = new();

        protected SmartDbContext _db;
        protected ICommonServices _services;
        protected ILocalizedEntityService _localizedEntityService;
        protected IStoreMappingService _storeMappingService;
        protected IUrlService _urlService;

        protected EntityImporterBase(
            ICommonServices services,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            IUrlService urlService)
        {
            _db = services.DbContext;
            _services = services;
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

        /// <summary>
        /// Imports localized properties.
        /// </summary>
        /// <param name="context">Import execution context.</param>
        /// <param name="scope">Scope for database commit.</param>
        /// <param name="batch">Batch of source data.</param>
        /// <param name="localizableProperties">Keys of localized properties to be included.</param>
        /// <returns>The task result contains the number of state entries written to the database.</returns>
        protected virtual async Task<int> ProcessLocalizationsAsync<TEntity>(
            ImportExecuteContext context,
            DbContextScope scope,
            IEnumerable<ImportRow<TEntity>> batch,
            string keyGroup,
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
                            var localizedProperty = collection.Find(language.Id, row.Entity.Id, prop);

                            if (localizedProperty != null && localizedProperty.Id != 0)
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
                return await scope.CommitAsync(context.CancelToken);
            }

            return 0;
        }

        /// <summary>
        /// Imports store mappings.
        /// </summary>
        /// <param name="context">Import execution context.</param>
        /// <param name="scope">Scope for database commit.</param>
        /// <param name="batch">Batch of source data.</param>
        /// <returns>The task result contains the number of state entries written to the database.</returns>
        protected virtual async Task<int> ProcessStoreMappingsAsync<TEntity>(
            ImportExecuteContext context,
            DbContextScope scope,
            IEnumerable<ImportRow<TEntity>> batch,
            string entityName)
            where TEntity : BaseEntity, IStoreRestricted
        {
            var shouldSave = false;
            var entityIds = batch.Select(x => x.Entity.Id).ToArray();
            if (!entityIds.Any())
            {
                return 0;
            }

            var collection = await _storeMappingService.GetStoreMappingCollectionAsync(entityName, entityIds);

            foreach (var row in batch)
            {
                var storeIds = row.GetDataValue<List<int>>("StoreIds");
                var hasStoreIds = storeIds?.Any() ?? false;

                if (hasStoreIds && storeIds.Count == 1 && storeIds[0] == 0)
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
                return await scope.CommitAsync(context.CancelToken);
            }

            return 0;
        }

        /// <summary>
        /// Imports URL records.
        /// </summary>
        /// <param name="context">Import execution context.</param>
        /// <param name="batch">Batch of source data.</param>
        /// <param name="entityName">Name of the entity for which the slugs are intended.</param>
        /// <returns>The task result contains the number of state entries written to the database.</returns>
        protected virtual async Task<int> ProcessSlugsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            string entityName)
            where TEntity : BaseEntity, ISlugSupported
        {
            using var scope = _urlService.CreateBatchScope(_db);

            // TODO: (core) (perf) IUrlService.ValidateSlugAsync ignores prefetched data.
            // TODO: (core) throws SqlException "Cannot insert duplicate key row in object 'dbo.UrlRecord' with unique index 'IX_UrlRecord_Slug'" when inserting new categories\products.

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

        /// <summary>
        /// Creates a download manager item.
        /// </summary>
        /// <param name="context">Import execution context.</param>
        /// <param name="urlOrPath">URL or path to download.</param>
        /// <param name="displayOrder">Display order of the item.</param>
        /// <returns>Download manager item.</returns>
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
                        item.FileName = WebHelper.GetFileNameFromUrl(urlOrPath) ?? Path.GetRandomFileName();
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

        /// <summary>
        /// Gets a value indicating whether the download succeeded.
        /// </summary>
        /// <param name="item">Download manager item.</param>
        /// <param name="context">Import execution context.</param>
        /// <returns>A value indicating whether the download succeeded.</returns>
        protected virtual bool FileDownloadSucceeded(DownloadManagerItem item, ImportExecuteContext context)
        {
            if (item.Success && File.Exists(item.Path))
            {
                // "Cache" URL to not download it again during this batch.
                if (item.Url.HasValue() && !_downloadedItems.ContainsKey(item.Url))
                {
                    _downloadedItems[item.Url] = Path.GetFileName(item.Path);
                }

                return true;
            }
            else
            {
                if (item.ErrorMessage.HasValue())
                {
                    context.Log.Error(new Exception(item.ErrorMessage), item.ToString());
                }

                return false;
            }
        }

        /// <summary>
        /// Converts a raw import value and returns <c>null</c> for a value of zero.
        /// </summary>
        /// <param name="value">Import value.</param>
        /// <param name="culture">Culture info.</param>
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
