using System;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Threading;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter : IDataExporter
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICommonServices _services;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;

        private readonly CatalogSettings _catalogSettings;

        public DataExporter(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICommonServices services,
            IProductService productService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ITaxService taxService,
            ICurrencyService currencyService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _services = services;
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxService = taxService;
            _currencyService = currencyService;
            _catalogSettings = catalogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        /// <summary>
        /// The name of the public export folder.
        /// </summary>
        public static string PublicFolder => "Exchange";

        /// <summary>
        /// The page size for loading data from database during export.
        /// </summary>
        public static int PageSize => 100;

        public async Task<DataExportResult> ExportAsync(DataExportRequest request, CancellationToken cancellationToken)
        {
            var ctx = new DataExporterContext(request, false, cancellationToken);

            if (request?.Profile?.Enabled ?? false)
            {
                var lockKey = $"dataexporter:profile:{request.Profile.Id}";
                if (!AsyncLock.IsLockHeld(lockKey))
                {
                    using (await AsyncLock.KeyedAsync(lockKey, cancellationToken: cancellationToken))
                    {
                        // TODO: (mg) (core) start to export in ExportAsync.
                        //await ExportCoreOuterAsync(ctx);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
                else
                {
                    ctx.Result.LastError = $"The execution of the profile \"{request.Profile.Name.NaIfEmpty()}\" (ID {request.Profile.Id}) is locked.";
                }
            }

            return ctx.Result;
        }

        public Task<DataExportPreviewResult> PreviewAsync(DataExportRequest request, int pageIndex)
        {
            throw new NotImplementedException();
        }

        #region Core export methods

        #endregion

        #region Utilities

        #endregion
    }
}
