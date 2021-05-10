using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export.Internal;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Threading;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter : IDataExporter
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ITaxCalculator _taxCalculator;

        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SeoSettings _seoSettings;

        public DataExporter(
            SmartDbContext db,
            ICommonServices services,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ICurrencyService currencyService,
            IMediaService mediaService,
            ProductUrlHelper productUrlHelper,
            ITaxCalculator taxCalculator,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            SeoSettings seoSettings)
        {
            _db = db;
            _services = services;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _productUrlHelper = productUrlHelper;
            _taxCalculator = taxCalculator;

            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            _seoSettings = seoSettings;
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
                        await ExportCore(ctx);
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

        private async Task<List<Store>> Init(DataExporterContext ctx)
        {
            List<Store> result = null;
            var priceDisplay = ctx.Projection.PriceType ?? _catalogSettings.PriceDisplayType;

            ctx.ContextCurrency = (await _db.Currencies.FindByIdAsync(ctx.Projection.CurrencyId ?? 0, false)) ?? _services.WorkContext.WorkingCurrency;
            ctx.ContextCustomer = (await _db.Customers.FindByIdAsync(ctx.Projection.CustomerId ?? 0, false)) ?? _services.WorkContext.CurrentCustomer;
            ctx.ContextLanguage = (await _db.Languages.FindByIdAsync(ctx.Projection.LanguageId ?? 0, false)) ?? _services.WorkContext.WorkingLanguage;

            ctx.Stores = _services.StoreContext.GetAllStores().ToDictionarySafe(x => x.Id, x => x);
            ctx.Languages = await _db.Languages.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);

            // TODO: (mg) (core) should be done later when batch context is available.
            ctx.PriceCalculationOptions = _priceCalculationService.CreateDefaultOptions(false, ctx.ContextCustomer, ctx.ContextCurrency, ctx.ProductBatchContext);
            ctx.PriceCalculationOptions.DetermineLowestPrice = priceDisplay == PriceDisplayType.LowestPrice;
            ctx.PriceCalculationOptions.DeterminePreselectedPrice = priceDisplay == PriceDisplayType.PreSelectedPrice;
            ctx.PriceCalculationOptions.ApplyPreselectedAttributes = priceDisplay == PriceDisplayType.PreSelectedPrice;

            if (ctx.Projection.ConvertNetToGrossPrices)
            {
                ctx.PriceCalculationOptions.TaxInclusive = true;
            }



            //...

            return result;
        }

        private Task ExportCore(DataExporterContext ctx)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Utilities

        #endregion
    }
}
