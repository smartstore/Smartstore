using System.Collections.Frozen;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Import.Events;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.DataExchange.Import
{
    public class CategoryImporter : EntityImporterBase
    {
        const string CargoDataKey = "CategoryImporter.CargoData";
        const string TargetCategoryIdsKey = "CategoryImporter.TargetCategoryIds";
        const string ParentCategoryIdsKey = "CategoryImporter.ParentCategoryIds";

        private static readonly FrozenDictionary<string, Expression<Func<Category, string>>> _localizableProperties = new Dictionary<string, Expression<Func<Category, string>>>()
        {
            { nameof(Category.Name), x => x.Name },
            { nameof(Category.FullName), x => x.FullName },
            { nameof(Category.Description), x => x.Description },
            { nameof(Category.BottomDescription), x => x.BottomDescription },
            { nameof(Category.MetaKeywords), x => x.MetaKeywords },
            { nameof(Category.MetaDescription), x => x.MetaDescription },
            { nameof(Category.MetaTitle), x => x.MetaTitle }
        }.ToFrozenDictionary();

        private readonly IMediaImporter _mediaImporter;

        public CategoryImporter(
            ICommonServices services,
            IStoreMappingService storeMappingService,
            IUrlService urlService,
            IMediaImporter mediaImporter,
            SeoSettings seoSettings)
            : base(services, storeMappingService, urlService, seoSettings)
        {
            _mediaImporter = mediaImporter;
        }

        public static string[] SupportedKeyFields => new[] { nameof(Category.Id), nameof(Category.Name) };
        public static string[] DefaultKeyFields => new[] { nameof(Category.Name), nameof(Category.Id) };

        protected override async Task ProcessBatchAsync(ImportExecuteContext context, CancellationToken cancelToken = default)
        {
            var entityName = nameof(Category);
            var cargo = await GetCargoData(context);
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<Category>();

            using (var scope = new DbContextScope(_services.DbContext, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows);

                // ===========================================================================
                // Process categories.
                // ===========================================================================
                var savedCategories = 0;
                try
                {
                    savedCategories = await ProcessCategoriesAsync(context, cargo, scope, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessCategoriesAsync));
                }

                if (savedCategories > 0)
                {
                    // Hooks are disabled but category tree may have changed.
                    context.ClearCache = true;
                }

                // Reduce batch to saved (valid) categories.
                // No need to perform import operations on errored categories.
                batch = batch.Where(x => x.Entity != null && !x.IsTransient).ToArray();

                // Update result object.
                context.Result.NewRecords += batch.Count(x => x.IsNew && !x.IsTransient);
                context.Result.ModifiedRecords += batch.Count(x => !x.IsNew && !x.IsTransient);

                // ===========================================================================
                // Process SEO slugs.
                // ===========================================================================
                if (segmenter.HasColumn("SeName", true) || batch.Any(x => x.IsNew || x.NameChanged))
                {
                    try
                    {
                        scope.DbContext.SuppressCommit = false;
                        await ProcessSlugsAsync(context, batch, entityName);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessSlugsAsync));
                    }
                    finally
                    {
                        scope.DbContext.SuppressCommit = true;
                    }
                }

                // ===========================================================================
                // Process store mappings.
                // ===========================================================================
                if (segmenter.HasColumn("StoreIds"))
                {
                    try
                    {
                        await ProcessStoreMappingsAsync(context, scope, batch, entityName);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessStoreMappingsAsync));
                    }
                }

                // ===========================================================================
                // Process localizations.
                // ===========================================================================
                try
                {
                    await ProcessLocalizationsAsync(context, scope, batch, entityName, _localizableProperties);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessLocalizationsAsync));
                }

                // ===========================================================================
                // Process pictures.
                // ===========================================================================
                if (segmenter.HasColumn("ImageUrl") && !segmenter.IsIgnored("PictureId"))
                {
                    try
                    {
                        cargo.NumberOfNewImages += await ProcessPicturesAsync(context, scope, batch);
                    }
                    catch (Exception ex)
                    {
                        context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessPicturesAsync));
                    }
                }

                if (segmenter.IsLastSegment || context.Abort == DataExchangeAbortion.Hard)
                {
                    // ===========================================================================
                    // Process parent category mappings.
                    // ===========================================================================
                    if (segmenter.HasColumn(nameof(Category.Id)) &&
                        segmenter.HasColumn(nameof(Category.ParentId)) &&
                        !segmenter.IsIgnored(nameof(Category.ParentId)))
                    {
                        try
                        {
                            // We can make the parent category assignment only after all the data has been processed and imported.
                            await ProcessParentMappingsAsync(context, scope, batch);
                        }
                        catch (Exception ex)
                        {
                            context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessParentMappingsAsync));
                        }
                    }

                    if (cargo.NumberOfNewImages > 0)
                    {
                        context.Result.AddInfo("Importing new images may result in image duplicates if TinyImage is installed or the images are larger than \"Maximum image size\" setting.");
                    }
                }
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<Category>(context, batch), cancelToken);
        }

        protected virtual async Task<int> ProcessCategoriesAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Category>> batch)
        {
            var defaultTemplateId = cargo.TemplateViewPaths["CategoryTemplate.ProductsInGridOrLines"];
            var hasNameColumn = context.DataSegmenter.HasColumn("Name");
            var parentCategoryIds = context.GetCustomProperty<Dictionary<int, int>>(ParentCategoryIdsKey);

            foreach (var row in batch)
            {
                Category category = null;
                var id = row.GetDataValue<int>(nameof(Category.Id));
                var name = row.GetDataValue<string>(nameof(Category.Name));

                foreach (var keyName in context.KeyFieldNames)
                {
                    switch (keyName)
                    {
                        case nameof(Category.Id):
                            category = await _db.Categories.FindByIdAsync(id, true, context.CancelToken);
                            break;
                        case nameof(Category.Name):
                            if (name.HasValue())
                                category = await _db.Categories.FirstOrDefaultAsync(x => x.Name == name, context.CancelToken);
                            break;
                    }

                    if (category != null)
                        break;
                }

                if (category == null)
                {
                    if (context.UpdateOnly)
                    {
                        ++context.Result.SkippedRecords;
                        continue;
                    }

                    // A name is required for new categories.
                    if (!row.HasDataValue(nameof(Category.Name)))
                    {
                        ++context.Result.SkippedRecords;
                        context.Result.AddMissingFieldError(row.RowInfo, nameof(Category), nameof(Category.Name));
                        continue;
                    }

                    category = new Category();
                }

                row.Initialize(category, name ?? category.Name);

                if (!row.IsNew && hasNameColumn && !category.Name.EqualsNoCase(name))
                {
                    // Perf: use this later for SeName updates.
                    row.NameChanged = true;
                }

                row.SetProperty(context.Result, (x) => x.Name);
                row.SetProperty(context.Result, (x) => x.FullName);
                row.SetProperty(context.Result, (x) => x.Description);
                row.SetProperty(context.Result, (x) => x.BottomDescription);
                row.SetProperty(context.Result, (x) => x.MetaKeywords);
                row.SetProperty(context.Result, (x) => x.MetaDescription);
                row.SetProperty(context.Result, (x) => x.MetaTitle);
                row.SetProperty(context.Result, (x) => x.PageSize);
                row.SetProperty(context.Result, (x) => x.AllowCustomersToSelectPageSize);
                row.SetProperty(context.Result, (x) => x.PageSizeOptions);
                row.SetProperty(context.Result, (x) => x.ShowOnHomePage);
                row.SetProperty(context.Result, (x) => x.HasDiscountsApplied);
                row.SetProperty(context.Result, (x) => x.Published, true);
                row.SetProperty(context.Result, (x) => x.DisplayOrder);
                row.SetProperty(context.Result, (x) => x.Alias);
                row.SetProperty(context.Result, (x) => x.DefaultViewMode);
                // With new entities, "LimitedToStores" is an implicit field, meaning
                // it has to be set to true by code if it's absent but "StoreIds" exists.
                row.SetProperty(context.Result, (x) => x.LimitedToStores, !row.GetDataValue<List<int>>("StoreIds").IsNullOrEmpty());

                if (row.TryGetDataValue("CategoryTemplateViewPath", out string tvp, row.IsTransient))
                {
                    category.CategoryTemplateId = tvp.HasValue() && cargo.TemplateViewPaths.ContainsKey(tvp)
                        ? cargo.TemplateViewPaths[tvp]
                        : defaultTemplateId;
                }

                if (row.IsTransient)
                {
                    // Only update parent category relationship if child and parent were inserted.
                    if (row.TryGetDataValue(nameof(Category.ParentId), out int parentId) && parentId > 0 && id > 0)
                    {
                        parentCategoryIds[id] = parentId;
                    }

                    _db.Categories.Add(category);
                }
                else
                {
                    category.UpdatedOnUtc = DateTime.UtcNow;
                }
            }

            // Commit whole batch at once.
            var num = await scope.CommitAsync(context.CancelToken);

            // Get new category ids.
            // Required for parent category relationship.
            var targetCategoryIds = context.GetCustomProperty<Dictionary<int, int>>(TargetCategoryIdsKey);

            foreach (var row in batch.Where(x => x.Entity != null))
            {
                var id = row.GetDataValue<int>(nameof(Category.Id));
                if (id != 0)
                {
                    targetCategoryIds[id] = row.Entity.Id;
                }
            }

            return num;
        }

        protected virtual async Task<int> ProcessPicturesAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Category>> batch)
        {
            _mediaImporter.MessageHandler ??= (msg, item) =>
            {
                AddMessage<Category>(msg, item, context);
            };

            var items = batch
                .Select(row => new
                {
                    Row = row,
                    Url = row.GetDataValue<string>("ImageUrl")
                })
                .Where(x => x.Url.HasValue())
                .Select(x => _mediaImporter.CreateDownloadItem(context.ImageDirectory, context.ImageDownloadDirectory, x.Row.Entity, x.Url, x.Row, 1))
                .ToList();

            return await _mediaImporter.ImportCategoryImagesAsync(scope, items, DuplicateFileHandling.Rename, context.CancelToken);
        }

        protected virtual async Task<int> ProcessParentMappingsAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Category>> batch)
        {
            var parentCategoryIds = context.GetCustomProperty<Dictionary<int, int>>(ParentCategoryIdsKey);
            if (!parentCategoryIds.Any())
            {
                return 0;
            }

            var categoryIds = context.GetCustomProperty<Dictionary<int, int>>(TargetCategoryIdsKey);
            var newIds = new Dictionary<int, int>();
            var num = 0;

            // Get new IDs.
            foreach (var item in parentCategoryIds)
            {
                // Find new parent category ID.
                if (categoryIds.TryGetValue(item.Value/* old parent ID */, out var newParentId) && newParentId > 0)
                {
                    // Find new child category ID.
                    if (categoryIds.TryGetValue(item.Key/* old child ID */, out var newChildId) && newChildId > 0)
                    {
                        newIds[newChildId] = newParentId;
                    }
                }
            }

            // Update ParentGroupedProductId using batches.
            parentCategoryIds.Clear();
            var childIds = newIds.Keys.ToArray();

            foreach (var childIdsChunk in childIds.Chunk(100))
            {
                var childCategories = await _db.Categories
                    .AsQueryable()
                    .Where(x => childIdsChunk.Contains(x.Id))
                    .ToListAsync(context.CancelToken);

                foreach (var childCategory in childCategories)
                {
                    if (newIds.TryGetValue(childCategory.Id, out var parentId) && parentId > 0)
                    {
                        childCategory.ParentId = parentId;
                    }
                }

                num += await scope.CommitAsync(context.CancelToken);
            }

            return num;
        }

        private async Task<ImporterCargoData> GetCargoData(ImportExecuteContext context)
        {
            if (context.CustomProperties.TryGetValue(CargoDataKey, out object value))
            {
                return (ImporterCargoData)value;
            }

            var categoryTemplates = await _db.CategoryTemplates
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(context.CancelToken);

            // Do not pass entities here because of batch scope!
            var result = new ImporterCargoData
            {
                TemplateViewPaths = categoryTemplates.ToDictionarySafe(x => x.ViewPath, x => x.Id)
            };

            context.CustomProperties[CargoDataKey] = result;
            return result;
        }

        /// <summary>
        /// Perf: contains data that is loaded once per import.
        /// </summary>
        protected class ImporterCargoData
        {
            public Dictionary<string, int> TemplateViewPaths { get; init; }
            public int NumberOfNewImages { get; set; }
        }
    }
}
