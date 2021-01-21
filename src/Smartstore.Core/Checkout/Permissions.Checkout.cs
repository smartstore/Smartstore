namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static class Order
        {
            public const string Self = "order";
            public const string Read = "order.read";
            public const string Update = "order.update";
            public const string Create = "order.create";
            public const string Delete = "order.delete";
            public const string EditItem = "order.editorderitem";
            public const string EditShipment = "order.editshipment";
            public const string EditRecurringPayment = "order.editrecurringpayment";

            public static class GiftCard
            {
                public const string Self = "order.giftcard";
                public const string Read = "order.giftcard.read";
                public const string Update = "order.giftcard.update";
                public const string Create = "order.giftcard.create";
                public const string Delete = "order.giftcard.delete";
                public const string Notify = "order.giftcard.notify";
            }

            public static class ReturnRequest
            {
                public const string Self = "order.returnrequest";
                public const string Read = "order.returnrequest.read";
                public const string Update = "order.returnrequest.update";
                public const string Create = "order.returnrequest.create";
                public const string Delete = "order.returnrequest.delete";
                public const string Accept = "order.returnrequest.accept";
            }
        }

        public static partial class Promotion
        {
            public const string Self = "promotion";

            public static class Affiliate
            {
                public const string Self = "promotion.affiliate";
                public const string Read = "promotion.affiliate.read";
                public const string Update = "promotion.affiliate.update";
                public const string Create = "promotion.affiliate.create";
                public const string Delete = "promotion.affiliate.delete";
            }
        }

        public static partial class Configuration
        {
            public static class PaymentMethod
            {
                public const string Self = "configuration.paymentmethod";
                public const string Read = "configuration.paymentmethod.read";
                public const string Update = "configuration.paymentmethod.update";
                public const string Activate = "configuration.paymentmethod.activate";
            }

            public static class Shipping
            {
                public const string Self = "configuration.shipping";
                public const string Read = "configuration.shipping.read";
                public const string Update = "configuration.shipping.update";
                public const string Create = "configuration.shipping.create";
                public const string Delete = "configuration.shipping.delete";
                public const string Activate = "configuration.shipping.activate";
            }

            public static class Tax
            {
                public const string Self = "configuration.tax";
                public const string Read = "configuration.tax.read";
                public const string Update = "configuration.tax.update";
                public const string Create = "configuration.tax.create";
                public const string Delete = "configuration.tax.delete";
                public const string Activate = "configuration.tax.activate";
            }
        }

        public static class Cart
        {
            public const string Self = "cart";
            public const string Read = "cart.read";
            public const string AccessShoppingCart = "cart.accessshoppingcart";
            public const string AccessWishlist = "cart.accesswishlist";

            public static class CheckoutAttribute
            {
                public const string Self = "cart.checkoutattribute";
                public const string Read = "cart.checkoutattribute.read";
                public const string Update = "cart.checkoutattribute.update";
                public const string Create = "cart.checkoutattribute.create";
                public const string Delete = "cart.checkoutattribute.delete";
            }
        }
    }
}
