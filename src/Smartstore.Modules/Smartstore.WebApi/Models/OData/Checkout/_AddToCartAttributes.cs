#nullable enable

using System.Runtime.Serialization;
using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Models.Checkout
{
    [DataContract]
    public partial class AddToCartAttributes
    {
        [DataMember(Name = "variants")]
        public List<ProductVariantQueryItem> Variants { get; set; } = new();

    }
}
