using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Web.Components;

namespace Smartstore.Admin.Components
{
    public class DashboardBestsellersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly MediaSettings _mediaSettings;

        public DashboardBestsellersViewComponent(SmartDbContext db, MediaSettings mediaSettings)
        {
            _db = db;
            _mediaSettings = mediaSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var pageSize = 7;
            var reportByQuantity = await _db.OrderItems
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByQuantityDesc)
                .Take(pageSize)
                .ToListAsync();

            var reportByAmount = await _db.OrderItems
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByAmountDesc)
                .Take(pageSize)
                .ToListAsync();

            var model = new DashboardBestsellersModel
            {
                BestsellersByQuantity = await GetBestsellersBriefReportModelAsync(reportByQuantity),
                BestsellersByAmount = await GetBestsellersBriefReportModelAsync(reportByAmount)
            };

            return View(model);
        }

        private async Task<IList<BestsellersReportLineModel>> GetBestsellersBriefReportModelAsync(List<BestsellersReportLine> report, bool includeFiles = false)
        {
            var productIds = report.Select(x => x.ProductId).Distinct().ToArray();
            var products = await _db.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            Dictionary<int, MediaFileInfo> files = null;

            if (includeFiles)
            {
                var fileIds = products.Values
                    .Select(x => x.MainPictureId ?? 0)
                    .Where(x => x != 0)
                    .Distinct()
                    .ToArray();

                files = (await Services.MediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);
            }

            var model = report.Select(x =>
            {
                var m = new BestsellersReportLineModel
                {
                    ProductId = x.ProductId,
                    TotalAmount = Services.CurrencyService.PrimaryCurrency.AsMoney(x.TotalAmount).ToString(),
                    TotalQuantity = x.TotalQuantity.ToString("N0")
                };

                var product = products.Get(x.ProductId);
                if (product != null)
                {
                    var file = files?.Get(product.MainPictureId ?? 0);

                    m.ProductName = product.Name;
                    m.ProductTypeName = product.GetProductTypeLabel(Services.Localization);
                    m.ProductTypeLabelHint = product.ProductTypeLabelHint;
                    m.Sku = product.Sku;
                    m.StockQuantity = product.StockQuantity;
                    m.Price = product.Price;
                    m.PictureThumbnailUrl = Services.MediaService.GetUrl(file, _mediaSettings.CartThumbPictureSize, null, false);
                    m.NoThumb = file == null;
                }

                return m;
            })
            .ToList();

            return model;
        }
    }
}
