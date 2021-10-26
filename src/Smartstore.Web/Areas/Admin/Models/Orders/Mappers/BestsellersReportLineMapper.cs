using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Common;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;

namespace Smartstore.Admin.Models.Orders
{
    internal static partial class BestsellersReportLineMappingExtensions
    {
        public static async Task<IList<BestsellersReportLineModel>> MapAsync(this IEnumerable<BestsellersReportLine> lines, 
            SmartDbContext db,
            bool includeFiles = false)
        {
            Guard.NotNull(lines, nameof(lines));

            var productIds = lines.ToDistinctArray(x => x.ProductId);
            var products = await db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            dynamic parameters = new ExpandoObject();
            parameters.Products = products;
            parameters.IncludeFiles = includeFiles;

            var models = await lines
                .SelectAsync(async x =>
                {
                    var model = new BestsellersReportLineModel();
                    await MapperFactory.MapAsync(x, model, parameters);
                    return model;
                })
                .AsyncToList();

            return models;
        }

        public static async Task<BestsellersReportLineModel> MapAsync(this BestsellersReportLine line,
            Dictionary<int, Product> products,
            bool includeFiles = false)
        {
            var model = new BestsellersReportLineModel();
            await line.MapAsync(model, products, includeFiles);

            return model;
        }

        public static async Task MapAsync(this BestsellersReportLine line,
            BestsellersReportLineModel model,
            Dictionary<int, Product> products,
            bool includeFiles = false)
        {
            Guard.NotNull(products, nameof(products));

            dynamic parameters = new ExpandoObject();
            parameters.Products = products;
            parameters.IncludeFiles = includeFiles;

            await MapperFactory.MapAsync(line, model, parameters);
        }
    }

    internal class BestsellersReportLineMapper : Mapper<BestsellersReportLine, BestsellersReportLineModel>
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly MediaSettings _mediaSettings;

        public BestsellersReportLineMapper(
            SmartDbContext db, 
            ICommonServices services,
            MediaSettings mediaSettings)
        {
            _db = db;
            _services = services;
            _mediaSettings = mediaSettings;
        }

        protected override void Map(BestsellersReportLine from, BestsellersReportLineModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(BestsellersReportLine from, BestsellersReportLineModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var includeFiles = (bool)parameters.IncludeFiles;
            var products = parameters.Products as Dictionary<int, Product>;
            var product = products.Get(from.ProductId);
            Dictionary<int, MediaFileInfo> files = null;

            if (includeFiles)
            {
                var fileIds = products.Values
                    .Select(x => x.MainPictureId ?? 0)
                    .Where(x => x != 0)
                    .Distinct()
                    .ToArray();

                files = (await _services.MediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);
            }

            to.ProductId = from.ProductId;
            to.TotalAmount = new Money(from.TotalAmount, _services.CurrencyService.PrimaryCurrency);
            to.TotalQuantity = from.TotalQuantity.ToString("N0");

            if (product != null)
            {
                var file = files?.Get(product.MainPictureId ?? 0);

                to.ProductName = product.Name;
                to.ProductTypeName = product.GetProductTypeLabel(_services.Localization);
                to.ProductTypeLabelHint = product.ProductTypeLabelHint;
                to.Sku = product.Sku;
                to.StockQuantity = product.StockQuantity;
                to.Price = product.Price;
                to.PictureThumbnailUrl = _services.MediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize, null, false);
                to.NoThumb = file == null;
            }
        }
    }
}
