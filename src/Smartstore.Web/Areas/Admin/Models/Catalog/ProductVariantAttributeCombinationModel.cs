using System.ComponentModel.DataAnnotations;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Content.Media;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.Fields.")]
    public class ProductVariantAttributeCombinationModel : EntityModelBase
    {
        [LocalizedDisplay("*StockQuantity")]
        public int StockQuantity { get; set; }

        [LocalizedDisplay("*AllowOutOfStockOrders")]
        public bool AllowOutOfStockOrders { get; set; }

        [LocalizedDisplay("*Sku")]
        public string Sku { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Gtin")]
        public string Gtin { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.ManufacturerPartNumber")]
        public string ManufacturerPartNumber { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Price")]
        public decimal? Price { get; set; }

        [UIHint("DeliveryTimes")]
        [LocalizedDisplay("Admin.Catalog.Products.Fields.DeliveryTime")]
        public int? DeliveryTimeId { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.QuantityUnit")]
        public int? QuantityUnitId { get; set; }

        [LocalizedDisplay("*Pictures")]
        public int[] AssignedPictureIds { get; set; } = Array.Empty<int>();

        public List<PictureSelectItemModel> AssignablePictures { get; set; } = new();

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Length")]
        public decimal? Length { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Width")]
        public decimal? Width { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.Height")]
        public decimal? Height { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.BasePriceAmount")]
        public decimal? BasePriceAmount { get; set; }

        [LocalizedDisplay("Admin.Catalog.Products.Fields.BasePriceBaseAmount")]
        public int? BasePriceBaseAmount { get; set; }

        [LocalizedDisplay("Common.IsActive")]
        public bool IsActive { get; set; }

        public List<ProductVariantAttributeModel> ProductVariantAttributes { get; set; } = new();

        [LocalizedDisplay("*Attributes")]
        public string AttributesXml { get; set; }

        [LocalizedDisplay("Common.Product")]
        public string ProductUrl { get; set; }

        public List<string> Warnings { get; set; } = new();

        public int ProductId { get; set; }
        public string PrimaryStoreCurrencyCode { get; set; }
        public string BaseDimensionIn { get; set; }

        #region Nested classes

        public class PictureSelectItemModel : EntityModelBase
        {
            public bool IsAssigned { get; set; }

            public MediaFileInfo Media { get; set; }
        }

        public class ProductVariantAttributeModel : EntityModelBase
        {
            public ProductVariantAttributeModel()
            {
                Values = new List<ProductVariantAttributeValueModel>();
            }

            public int ProductAttributeId { get; set; }

            public string Name { get; set; }

            public string TextPrompt { get; set; }

            public bool IsRequired { get; set; }

            public AttributeControlType AttributeControlType { get; set; }

            public IList<ProductVariantAttributeValueModel> Values { get; set; }

            public string GetControlId(int productId, int bundleItemId)
            {
                return ProductVariantQueryItem.CreateKey(productId, bundleItemId, ProductAttributeId, Id);
            }
        }

        public class ProductVariantAttributeValueModel : EntityModelBase
        {
            public string Name { get; set; }

            public bool IsPreSelected { get; set; }
        }

        #endregion
    }

    [Mapper(Lifetime = ServiceLifetime.Singleton)]
    public class ProductVariantAttributeCombinationMapper :
        IMapper<ProductVariantAttributeCombination, ProductVariantAttributeCombinationModel>,
        IMapper<ProductVariantAttributeCombinationModel, ProductVariantAttributeCombination>
    {
        public Task MapAsync(ProductVariantAttributeCombination from, ProductVariantAttributeCombinationModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.AssignedPictureIds = from.GetAssignedMediaIds();
            return Task.CompletedTask;
        }

        public Task MapAsync(ProductVariantAttributeCombinationModel from, ProductVariantAttributeCombination to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SetAssignedMediaIds(from.AssignedPictureIds);
            return Task.CompletedTask;
        }
    }
}
