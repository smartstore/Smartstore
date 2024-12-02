using System.Collections.Frozen;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Import.Events;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.DataExchange.Import
{
    public class ManufacturerImporter : EntityImporterBase
    {
        const string CargoDataKey = "ManufacturerImporter.CargoData";

        private static readonly FrozenDictionary<string, Expression<Func<Manufacturer, string>>> _localizableProperties = new Dictionary<string, Expression<Func<Manufacturer, string>>>()
        {
            { nameof(Manufacturer.Name), x => x.Name },
            { nameof(Manufacturer.Description), x => x.Description },
            { nameof(Manufacturer.BottomDescription), x => x.BottomDescription },
            { nameof(Manufacturer.MetaKeywords), x => x.MetaKeywords },
            { nameof(Manufacturer.MetaDescription), x => x.MetaDescription },
            { nameof(Manufacturer.MetaTitle), x => x.MetaTitle }
        }.ToFrozenDictionary();

        private readonly IMediaImporter _mediaImporter;

        public ManufacturerImporter(
            ICommonServices services,
            IStoreMappingService storeMappingService,
            IUrlService urlService,
            IMediaImporter mediaImporter,
            SeoSettings seoSettings)
            : base(services, storeMappingService, urlService, seoSettings)
        {
            _mediaImporter = mediaImporter;
        }

        public static string[] SupportedKeyFields => [nameof(Manufacturer.Id), nameof(Manufacturer.Name)];
        public static string[] DefaultKeyFields => [nameof(Manufacturer.Name), nameof(Manufacturer.Id)];

        protected override async Task ProcessBatchAsync(ImportExecuteContext context, CancellationToken cancelToken = default)
        {
            var entityName = nameof(Manufacturer);
            var cargo = await GetCargoData(context);
            var segmenter = context.DataSegmenter;
            var batch = segmenter.GetCurrentBatch<Manufacturer>();

            using (var scope = new DbContextScope(_services.DbContext, autoDetectChanges: false, minHookImportance: HookImportance.Important, deferCommit: true))
            {
                await context.SetProgressAsync(segmenter.CurrentSegmentFirstRowIndex - 1, segmenter.TotalRows);

                // ===========================================================================
                // Process manufacturers.
                // ===========================================================================
                try
                {
                    _ = await ProcessManufacturersAsync(context, cargo, scope, batch);
                }
                catch (Exception ex)
                {
                    context.Result.AddError(ex, segmenter.CurrentSegment, nameof(ProcessManufacturersAsync));
                }

                // Reduce batch to saved (valid) manufacturers.
                // No need to perform import operations on errored manufacturers.
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
                if (segmenter.HasColumn("ImageUrl"))
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

                if ((segmenter.IsLastSegment || context.Abort == DataExchangeAbortion.Hard) && cargo.NumberOfNewImages > 0)
                {
                    context.Result.AddInfo("Importing new images may result in image duplicates if TinyImage is installed or the images are larger than \"Maximum image size\" setting.");
                }
            }

            await _services.EventPublisher.PublishAsync(new ImportBatchExecutedEvent<Manufacturer>(context, batch), cancelToken);
        }

        protected virtual async Task<int> ProcessManufacturersAsync(
            ImportExecuteContext context,
            ImporterCargoData cargo,
            DbContextScope scope,
            IEnumerable<ImportRow<Manufacturer>> batch)
        {
            var defaultTemplateId = cargo.TemplateViewPaths["ManufacturerTemplate.ProductsInGridOrLines"];
            var hasNameColumn = context.DataSegmenter.HasColumn("Name");

            foreach (var row in batch)
            {
                Manufacturer manufacturer = null;
                var id = row.GetDataValue<int>(nameof(Manufacturer.Id));
                var name = row.GetDataValue<string>(nameof(Manufacturer.Name));

                foreach (var keyName in context.KeyFieldNames)
                {
                    switch (keyName)
                    {
                        case nameof(Manufacturer.Id):
                            manufacturer = await _db.Manufacturers.FindByIdAsync(id, true, context.CancelToken);
                            break;
                        case nameof(Manufacturer.Name):
                            if (name.HasValue())
                            {
                                manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(x => x.Name == name, context.CancelToken);
                            }
                            break;
                    }

                    if (manufacturer != null)
                        break;
                }

                if (manufacturer == null)
                {
                    if (context.UpdateOnly)
                    {
                        ++context.Result.SkippedRecords;
                        continue;
                    }

                    // A name is required for new manufacturers
                    if (!row.HasDataValue(nameof(Manufacturer.Name)))
                    {
                        ++context.Result.SkippedRecords;
                        context.Result.AddMissingFieldError(row.RowInfo, nameof(Manufacturer), nameof(Manufacturer.Name));
                        continue;
                    }

                    manufacturer = new Manufacturer();
                }

                row.Initialize(manufacturer, name ?? manufacturer.Name);

                if (!row.IsNew && hasNameColumn && !manufacturer.Name.EqualsNoCase(name))
                {
                    // Perf: use this later for SeName updates.
                    row.NameChanged = true;
                }

                row.SetProperty(context.Result, (x) => x.Name);
                row.SetProperty(context.Result, (x) => x.Description);
                row.SetProperty(context.Result, (x) => x.BottomDescription);
                row.SetProperty(context.Result, (x) => x.MetaKeywords);
                row.SetProperty(context.Result, (x) => x.MetaDescription);
                row.SetProperty(context.Result, (x) => x.MetaTitle);
                row.SetProperty(context.Result, (x) => x.PageSize);
                row.SetProperty(context.Result, (x) => x.AllowCustomersToSelectPageSize);
                row.SetProperty(context.Result, (x) => x.PageSizeOptions);
                row.SetProperty(context.Result, (x) => x.Published, true);
                row.SetProperty(context.Result, (x) => x.DisplayOrder);

                // With new entities, "LimitedToStores" is an implicit field, meaning
                // it has to be set to true by code if it's absent but "StoreIds" exists.
                row.SetProperty(context.Result, (x) => x.LimitedToStores, !row.GetDataValue<List<int>>("StoreIds").IsNullOrEmpty());

                if (row.TryGetDataValue("ManufacturerTemplateViewPath", out string tvp, row.IsTransient))
                {
                    manufacturer.ManufacturerTemplateId = tvp.HasValue() && cargo.TemplateViewPaths.TryGetValue(tvp, out int value) ? value : defaultTemplateId;
                }

                if (row.IsTransient)
                {
                    _db.Manufacturers.Add(manufacturer);
                }
                else
                {
                    manufacturer.UpdatedOnUtc = DateTime.UtcNow;
                }
            }

            // Commit whole batch at once.
            var num = await scope.CommitAsync(context.CancelToken);
            return num;
        }

        protected virtual async Task<int> ProcessPicturesAsync(ImportExecuteContext context, DbContextScope scope, IEnumerable<ImportRow<Manufacturer>> batch)
        {
            _mediaImporter.MessageHandler ??= (msg, item) =>
            {
                AddMessage<Manufacturer>(msg, item, context);
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

            return await _mediaImporter.ImportManufacturerImagesAsync(scope, items, DuplicateFileHandling.Rename, context.CancelToken);
        }

        private async Task<ImporterCargoData> GetCargoData(ImportExecuteContext context)
        {
            if (context.CustomProperties.TryGetValue(CargoDataKey, out object value))
            {
                return (ImporterCargoData)value;
            }

            var templates = await _db.ManufacturerTemplates
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(context.CancelToken);

            // Do not pass entities here because of batch scope!
            var result = new ImporterCargoData
            {
                TemplateViewPaths = templates.ToDictionarySafe(x => x.ViewPath, x => x.Id)
            };

            context.CustomProperties[CargoDataKey] = result;
            return result;
        }

        protected class ImporterCargoData
        {
            public Dictionary<string, int> TemplateViewPaths { get; init; }
            public int NumberOfNewImages { get; set; }
        }
    }
}
