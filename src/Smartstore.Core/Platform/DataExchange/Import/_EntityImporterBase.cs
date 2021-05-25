using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Domain;
using Smartstore.IO;

namespace Smartstore.Core.DataExchange.Import
{
    public abstract class EntityImporterBase : IEntityImporter
    {
        public DateTime UtcNow { get; private set; }

        /// <summary>
        /// URL to file name map. To avoid downloading image several times.
        /// </summary>
		public Dictionary<string, string> DownloadedItems { get; private set; } = new();

        public IDirectory ImageDownloadFolder { get; private set; }

        public IDirectory ImageFolder { get; private set; }


        public Task ExecuteAsync(ImportExecuteContext context, CancellationToken cancelToken = default)
        {
            return ImportAsync(context, cancelToken);
        }

        /// <summary>
        /// Imports data to the database.
        /// </summary>
        /// <param name="context">Import execution context.</param>
        /// <param name="cancelToken">A cancellation token to cancel the import.</param>
        protected abstract Task ImportAsync(ImportExecuteContext context, CancellationToken cancelToken = default);

        protected void Initialize(ImportExecuteContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.ImportDirectory, nameof(context.ImportDirectory));

            UtcNow = DateTime.UtcNow;

            context.Result.TotalRecords = context.DataSegmenter.TotalRows;
        }

        protected virtual async Task<int> ProcessLocalizationsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            IDictionary<string, Expression<Func<TEntity, string>>> localizableProperties) 
            where TEntity : BaseEntity, ILocalizedEntity
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(batch, nameof(batch));
            Guard.NotNull(localizableProperties, nameof(localizableProperties));

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

            var localizedEntityService = context.Services.Resolve<ILocalizedEntityService>();
            var shouldSave = false;

            foreach (var row in batch)
            {
                foreach (var prop in localizedProps)
                {
                    var lambda = localizableProperties[prop];
                    foreach (var lang in context.Languages)
                    {
                        if (row.TryGetDataValue(prop /* ColumnName */, lang.UniqueSeoCode, out string value))
                        {
                            // TODO: (mg) (core) (perf) We have to work against prefetched values here (prefetch the whole batch).
                            // Reason: ApplyLocalizedValueAsync() always hits the database to determine whether the entity exists already.
                            // Keep in mind that this code here was build before localized property prefetching was available.
                            await localizedEntityService.ApplyLocalizedValueAsync(row.Entity, lambda, value, lang.Id);
                            shouldSave = true;
                        }
                    }
                }
            }

            if (shouldSave)
            {
                // Commit whole batch at once.
                return await context.Services.DbContext.SaveChangesAsync();
            }

            return 0;
        }

        protected virtual async Task<int> ProcessStoreMappingsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch) 
            where TEntity : BaseEntity, IStoreRestricted
        {
            var storeMappingService = context.Services.Resolve<IStoreMappingService>();
            var shouldSave = false;

            foreach (var row in batch)
            {
                var storeIds = row.GetDataValue<List<int>>("StoreIds");
                if (!storeIds.IsNullOrEmpty())
                {
                    await storeMappingService.ApplyStoreMappingsAsync(row.Entity, storeIds.ToArray());
                    shouldSave = true;
                }
            }

            if (shouldSave)
            {
                // Commit whole batch at once.
                return await context.Services.DbContext.SaveChangesAsync();
            }

            return 0;
        }

        protected virtual async Task<int> ProcessSlugsAsync<TEntity>(
            ImportExecuteContext context,
            IEnumerable<ImportRow<TEntity>> batch,
            string entityName) 
            where TEntity : BaseEntity, ISlugSupported
        {
            var urlService = context.Services.Resolve<IUrlService>();

            foreach (var row in batch)
            {
                try
                {
                    if (row.TryGetDataValue("SeName", out string seName) || row.IsNew || row.NameChanged)
                    {
                        var slugResult = await urlService.ValidateSlugAsync(row.Entity, seName, true);

                        if (row.IsNew)
                        {
                            context.Services.DbContext.UrlRecords.Add(new UrlRecord
                            {
                                EntityId = row.Entity.Id,
                                EntityName = entityName,
                                Slug = slugResult.Slug,
                                LanguageId = 0,
                                IsActive = true,
                            });
                        }
                        else
                        {
                            // Let us not save here, otherwise 'save' parameter makes no sense ;-)
                            await urlService.ApplySlugAsync(slugResult, false);
                        }

                        // Process localized slugs.
                        foreach (var language in context.Languages)
                        {
                            // ValidateSlugAsync has no 'name' parameter anymore.
                            // We ourselves must ensure that 'Name[<UniqueSeoCode>]' is taken into account.
                            if (!row.TryGetDataValue("SeName", language.UniqueSeoCode, out seName) || seName.IsEmpty())
                            {
                                row.TryGetDataValue("Name", language.UniqueSeoCode, out seName);
                            }

                            if (seName.HasValue())
                            {
                                slugResult = await urlService.ValidateSlugAsync(row.Entity, seName, false, language.Id);
                                await urlService.ApplySlugAsync(slugResult, false);
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
            return await context.Services.DbContext.SaveChangesAsync();
        }
    }
}
