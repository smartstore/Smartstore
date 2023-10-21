using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Cart
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this OrganizedShoppingCartItem entity, ImageModel model, int pictureSize, string productName)
        {
            Guard.NotNull(entity);
            Guard.NotNull(model);

            var product = entity.Item.Product;

            dynamic parameters = new GracefulDynamicObject();
            parameters.Selection = entity.Item.AttributeSelection;
            parameters.Product = product;
            parameters.ProductName = productName.HasValue() ? productName : product.GetLocalized(x => x.Name);
            parameters.PictureSize = pictureSize;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    public class CartItemImageMapper : Mapper<OrganizedShoppingCartItem, ImageModel>
    {
        private readonly SmartDbContext _db;
        private readonly IMediaService _mediaService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly Localizer T;

        public CartItemImageMapper(
            SmartDbContext db,
            IMediaService mediaService,
            IProductAttributeMaterializer productAttributeMaterializer,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            Localizer t)
        {
            _db = db;
            _mediaService = mediaService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            T = t;
        }

        protected override void Map(OrganizedShoppingCartItem from, ImageModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(OrganizedShoppingCartItem from, ImageModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var product = parameters?.Product as Product;
            var attributeSelection = parameters?.Selection as ProductVariantAttributeSelection;
            var pictureSize = parameters?.PictureSize as int? ?? _mediaSettings.CartThumbPictureSize;
            var productName = parameters?.ProductName as string ?? string.Empty;

            if (product == null)
            {
                return;
            }

            MediaFileInfo file = null;
            if (attributeSelection != null)
            {
                var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, attributeSelection);
                if (combination != null)
                {
                    var fileIds = combination.GetAssignedMediaIds();
                    if (fileIds.Any())
                    {
                        file = await _mediaService.GetFileByIdAsync(fileIds[0], MediaLoadFlags.AsNoTracking);
                    }
                }
            }

            // If no attribute combination image was found, then load product pictures.
            if (file == null)
            {
                var mediaFile = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(product.Id)
                    .FirstOrDefaultAsync();

                if (mediaFile?.MediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(mediaFile.MediaFile);
                }
            }

            // Let's check whether this product has some parent "grouped" product.
            if (file == null && product.Visibility == ProductVisibility.Hidden && product.ParentGroupedProductId > 0)
            {
                var mediaFile = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(product.ParentGroupedProductId)
                    .FirstOrDefaultAsync();

                if (mediaFile?.MediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(mediaFile.MediaFile);
                }
            }

            to.Populate(file, pictureSize);
            to.Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? T("Media.Product.ImageLinkTitleFormat", productName);
            to.Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? T("Media.Product.ImageAlternateTextFormat", productName);
            to.NoFallback = _catalogSettings.HideProductDefaultPictures;
        }
    }
}
