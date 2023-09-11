using Autofac;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Net.Http;

namespace Smartstore.Core.DataExchange.Import
{
    public abstract partial class EntityImporterBase : IEntityImporter
    {
        protected SmartDbContext _db;
        protected ICommonServices _services;
        protected IStoreMappingService _storeMappingService;
        protected IUrlService _urlService;
        protected SeoSettings _seoSettings;

        protected EntityImporterBase(
            ICommonServices services,
            IStoreMappingService storeMappingService,
            IUrlService urlService,
            SeoSettings seoSettings)
        {
            _db = services.DbContext;
            _services = services;
            _storeMappingService = storeMappingService;
            _urlService = urlService;
            _seoSettings = seoSettings;
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
            Guard.NotNull(context);
            Guard.NotNull(batch);
            Guard.NotNull(localizableProperties);

            var entityIds = batch.Select(x => x.Entity.Id).ToArray();
            if (entityIds.Length == 0)
            {
                return 0;
            }

            // Perf: determine whether our localizable properties actually have 
            // counterparts in the source BEFORE import batch begins. This way we spare ourself
            // to query over and over for values.
            var localizedProps = (from kvp in localizableProperties
                                  where context.DataSegmenter.GetColumnIndexes(kvp.Key).Length > 0
                                  select kvp.Key).ToArray();
            if (localizedProps.Length == 0)
            {
                return 0;
            }

            var shouldSave = false;
            var lpQuery = _db.LocalizedProperties.Where(x => x.LocaleKeyGroup == keyGroup && entityIds.Contains(x.EntityId));
            var collection = new LocalizedPropertyCollection(keyGroup, entityIds, await lpQuery.ToListAsync());

            foreach (var row in batch)
            {
                foreach (var prop in localizedProps)
                {
                    var keySelector = localizableProperties[prop];
                    foreach (var language in context.Languages)
                    {
                        if (TryGetLocalizedValue(row, prop /* ColumnName */, language, out string value))
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
                                var propInfo = keySelector.ExtractPropertyInfo() ?? throw new ArgumentException($"Expression '{keySelector}' does not refer to a property.");

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
                context.ClearCache = true;

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
            if (entityIds.Length == 0)
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

            foreach (var row in batch)
            {
                try
                {
                    if (row.TryGetDataValue("SeName", out string seName) || row.IsNew || row.NameChanged)
                    {
                        scope.ApplySlugs(new ValidateSlugResult
                        {
                            Source = row.Entity,
                            Slug = SlugUtility.Slugify(seName.NullEmpty() ?? row.EntityDisplayName, _seoSettings)
                        });
                    }

                    // Process localized slugs.
                    foreach (var language in context.Languages)
                    {
                        var hasSeName = TryGetLocalizedValue(row, "SeName", language, out seName);
                        var hasLocalizedName = TryGetLocalizedValue(row, "Name", language, out string localizedName);

                        if (hasSeName || hasLocalizedName)
                        {
                            scope.ApplySlugs(new ValidateSlugResult
                            {
                                Source = row.Entity,
                                Slug = SlugUtility.Slugify(seName.NullEmpty() ?? localizedName, _seoSettings),
                                LanguageId = language.Id
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Result.AddWarning(ex.Message, row.RowInfo, "SeName");
                }
            }

            // Commit whole batch at once.
            var num = await scope.CommitAsync(context.CancelToken);
            if (num != 0)
            {
                context.ClearCache = true;
            }

            return num;
        }

        /// <summary>
        /// Adds a message to <see cref="ImportExecuteContext.Result"/> and prevents from logging too many messages of the same reason.
        /// </summary>
        protected static void AddMessage<TEntity>(ImportMessage msg, DownloadManagerItem item, ImportExecuteContext context)
            where TEntity : BaseEntity
        {
            const int maxLogEntries = 20;

            var rowInfo = item?.State != null
                ? ((ImportRow<TEntity>)item.State).RowInfo
                : null;

            if (msg.Reason != ImportMessageReason.None)
            {
                var counts = context.GetCustomProperty<Dictionary<int, int>>("LoggedInfosCount");
                counts.TryGetValue((int)msg.Reason, out var count);

                if (count <= maxLogEntries)
                {
                    counts[(int)msg.Reason] = count + 1;

                    if (count < maxLogEntries)
                    {
                        context.Result.AddMessage(msg.Message, msg.MessageType, rowInfo);
                    }   
                    else
                    {
                        context.Result.AddMessage($"No further logging of '{msg.Reason}' messages. Reason: too many log entries.", ImportMessageType.Info, rowInfo);
                    }   
                }
            }
            else
            {
                context.Result.AddMessage(msg.Message, msg.MessageType, rowInfo);
            }
        }

        private static bool TryGetLocalizedValue<TEntity>(ImportRow<TEntity> row, string columnName, Language language, out string value)
            where TEntity : BaseEntity
        {
            if (row.TryGetDataValue(columnName, language.LanguageCulture, out value) || row.TryGetDataValue(columnName, language.UniqueSeoCode, out value))
            {
                return true;
            }

            return false;
        }
    }
}
