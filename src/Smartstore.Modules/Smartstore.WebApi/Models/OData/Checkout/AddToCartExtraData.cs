using System.Runtime.Serialization;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.GiftCards;

namespace Smartstore.Web.Api.Models.Checkout
{
    /// <summary>
    /// Represents optional extra data when adding an item to shopping cart or wishlist.
    /// </summary>
    [DataContract]
    public partial class AddToCartExtraData
    {
        /// <summary>
        /// Product attributes to apply. Requires product variant identifiers to specify.
        /// Alternatively, if you do not know the identifiers, then you can use **searchAttributes**.
        /// 
        /// Use **attributes** or **searchAttributes**, but not both together.
        /// Both do the same thing (applying attributes), just in different ways.
        /// </summary>
        [DataMember(Name = "attributes")]
        public List<ProductVariantQueryItem> Attributes { get; set; }

        /// <summary>
        /// Searches for attributes associated with the product and applies them.
        /// Simple attributes such as **color** or **size** can be specified more easily this way than via **attributes**.
        /// 
        /// Use **attributes** or **searchAttributes**, but not both together.
        /// Both do the same thing (applying attributes), just in different ways.
        /// </summary>
        [DataMember(Name = "searchAttributes")]
        public List<SearchAttribute> SearchAttributes { get; set; }

        /// <summary>
        /// Checkout attributes to apply. Requires checkout attribute identifiers to specify.
        /// Alternatively, if you do not know the identifiers, then you can use **searchCheckoutAttributes**.
        /// 
        /// Use **checkoutAttributes** or **searchCheckoutAttributes**, but not both together.
        /// Both do the same thing (applying checkout attributes), just in different ways.
        /// </summary>
        [DataMember(Name = "checkoutAttributes")]
        public List<CheckoutAttributeQueryItem> CheckoutAttributes { get; set; }

        /// <summary>
        /// Searches for checkout attributes and applies them.
        /// 
        /// Use **checkoutAttributes** or **searchCheckoutAttributes**, but not both together.
        /// Both do the same thing (applying checkout attributes), just in different ways.
        /// </summary>
        [DataMember(Name = "searchCheckoutAttributes")]
        public List<SearchAttribute> SearchCheckoutAttributes { get; set; }

        /// <summary>
        /// Gift card properties to apply. Only applicable if the product is a gift card.
        /// </summary>
        [DataMember(Name = "giftCard")]
        public GiftCardInfo GiftCard { get; set; }

        /// <summary>
        /// A price entered by customer. Only applicable if the product supports it.
        /// </summary>
        [DataMember(Name = "customerEnteredPrice")]
        public AddToCartPrice CustomerEnteredPrice { get; set; }

        /// <summary>
        /// Represents a price entered by customer.
        /// </summary>
        [DataContract]
        public class AddToCartPrice
        {
            /// <summary>
            /// Customer entered price. Only applicable if the product supports it.
            /// </summary>
            /// <example>9.89</example>
            [DataMember(Name = "price")]
            public decimal Price { get; set; }

            /// <summary>
            /// Currency code for **price**.
            /// If empty, then **price** must be in the primary currency of the store.
            /// </summary>
            /// <example>EUR</example>
            [DataMember(Name = "currencyCode")]
            public string CurrencyCode { get; set; }
        }

        /// <summary>
        /// Represents a product or checkout attribute to search for.
        /// </summary>
        [DataContract]
        public class SearchAttribute
        {
            /// <summary>
            /// The name of the attribute.
            /// </summary>
            /// <example>Color</example>
            [DataMember(Name = "name")]
            public string Name { get; set; }

            /// <summary>
            /// The value of the attribute.
            /// </summary>
            /// <example>Green</example>
            [DataMember(Name = "value")]
            public string Value { get; set; }
        }
    }
}
