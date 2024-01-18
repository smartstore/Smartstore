using System.Dynamic;
using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Content.Media;

namespace Smartstore.Admin.Models.Orders
{
    internal static partial class BestsellersReportLineMappingExtensions
    {
        public static async Task<IList<BestsellersReportLineModel>> MapAsync(this IEnumerable<BestsellersReportLine> lines,
            ICommonServices services,
            bool includeFiles = false)
        {
            Guard.NotNull(lines, nameof(lines));

            var productIds = lines.ToDistinctArray(x => x.ProductId);
            var products = await services.DbContext.Products
                .AsNoTracking()
                .Where(x => productIds.Contains(x.Id))
                .SelectSummary()
                .ToDictionaryAsync(x => x.Id);

            dynamic parameters = new ExpandoObject();
            parameters.Products = products;

            if (includeFiles)
            {
                var fileIds = products.Values
                    .Select(x => x.MainPictureId ?? 0)
                    .Where(x => x != 0)
                    .Distinct()
                    .ToArray();

                parameters.Files = (await services.MediaService.GetFilesByIdsAsync(fileIds)).ToDictionarySafe(x => x.Id);
            }
            else
            {
                parameters.Files = null;
            }

            var mapper = MapperFactory.GetMapper<BestsellersReportLine, BestsellersReportLineModel>();
            var models = await lines
                .SelectAwait(async x =>
                {
                    var model = new BestsellersReportLineModel();
                    await mapper.MapAsync(x, model, parameters);
                    return model;
                })
                .AsyncToList();

            return models;
        }

        public static async Task<BestsellersReportLineModel> MapAsync(this BestsellersReportLine line,
            Dictionary<int, Product> products,
            Dictionary<int, MediaFileInfo> files = null)
        {
            var model = new BestsellersReportLineModel();
            await line.MapAsync(model, products, files);

            return model;
        }

        public static async Task MapAsync(this BestsellersReportLine line,
            BestsellersReportLineModel model,
            Dictionary<int, Product> products,
            Dictionary<int, MediaFileInfo> files = null)
        {
            Guard.NotNull(products, nameof(products));

            dynamic parameters = new ExpandoObject();
            parameters.Products = products;
            parameters.Files = files;

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

            var products = parameters.Products as Dictionary<int, Product>;
            var product = products.Get(from.ProductId);

            if (product != null)
            {
                var files = parameters.Files as Dictionary<int, MediaFileInfo>;

                await product.MapAsync(to, files);
            }
            else
            {
                to.Id = from.ProductId;
            }

            to.TotalAmount = new Money(from.TotalAmount, _services.CurrencyService.PrimaryCurrency);
            to.TotalQuantity = from.TotalQuantity.ToString("N0");
        }
    }
}
