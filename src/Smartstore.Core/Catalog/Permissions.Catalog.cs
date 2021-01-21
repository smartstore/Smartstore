namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static class Catalog
        {
            public const string Self = "catalog";
            public const string DisplayPrice = "catalog.displayprice";

            public static class Product
            {
                public const string Self = "catalog.product";
                public const string Read = "catalog.product.read";
                public const string Update = "catalog.product.update";
                public const string Create = "catalog.product.create";
                public const string Delete = "catalog.product.delete";
                public const string EditCategory = "catalog.product.editcategory";
                public const string EditManufacturer = "catalog.product.editmanufacturer";
                public const string EditAssociatedProduct = "catalog.product.editassociatedproduct";
                public const string EditBundle = "catalog.product.editbundle";
                public const string EditAttribute = "catalog.product.editattribute";
                public const string EditVariant = "catalog.product.editvariant";
                public const string EditPromotion = "catalog.product.editpromotion";
                public const string EditPicture = "catalog.product.editpicture";
                public const string EditTag = "catalog.product.edittag";
                public const string EditTierPrice = "catalog.product.edittierprice";
            }

            public static class ProductReview
            {
                public const string Self = "catalog.productreview";
                public const string Read = "catalog.productreview.read";
                public const string Update = "catalog.productreview.update";
                public const string Create = "catalog.productreview.create";
                public const string Delete = "catalog.productreview.delete";
                public const string Approve = "catalog.productreview.Approve";
            }

            public static class Category
            {
                public const string Self = "catalog.category";
                public const string Read = "catalog.category.read";
                public const string Update = "catalog.category.update";
                public const string Create = "catalog.category.create";
                public const string Delete = "catalog.category.delete";
                public const string EditProduct = "catalog.category.editproduct";
            }

            public static class Manufacturer
            {
                public const string Self = "catalog.manufacturer";
                public const string Read = "catalog.manufacturer.read";
                public const string Update = "catalog.manufacturer.update";
                public const string Create = "catalog.manufacturer.create";
                public const string Delete = "catalog.manufacturer.delete";
                public const string EditProduct = "catalog.manufacturer.editproduct";
            }

            public static class Variant
            {
                public const string Self = "catalog.variant";
                public const string Read = "catalog.variant.read";
                public const string Update = "catalog.variant.update";
                public const string Create = "catalog.variant.create";
                public const string Delete = "catalog.variant.delete";
                public const string EditSet = "catalog.variant.editoptionset";
            }

            public static class Attribute
            {
                public const string Self = "catalog.attribute";
                public const string Read = "catalog.attribute.read";
                public const string Update = "catalog.attribute.update";
                public const string Create = "catalog.attribute.create";
                public const string Delete = "catalog.attribute.delete";
                public const string EditOption = "catalog.attribute.editoption";
            }
        }

        public static partial class Promotion
        {
            public static class Discount
            {
                public const string Self = "promotion.discount";
                public const string Read = "promotion.discount.read";
                public const string Update = "promotion.discount.update";
                public const string Create = "promotion.discount.create";
                public const string Delete = "promotion.discount.delete";
            }
        }
    }
}
