using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Smartstore.Core.Localization;
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


        public Task ExecuteAsync(ImportExecuteContext context)
        {
            return ImportAsync(context);
        }

        protected abstract Task ImportAsync(ImportExecuteContext context);

        protected void Initialize(ImportExecuteContext context)
        {
            UtcNow = DateTime.UtcNow;

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

    }
}
