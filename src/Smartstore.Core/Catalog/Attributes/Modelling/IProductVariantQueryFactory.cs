using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Catalog.Attributes.Modelling
{
    /// <summary>
    /// Query factory for product variants.
    /// </summary>
    public partial interface IProductVariantQueryFactory
    {
        /// <summary>
        /// The last created query instance. The model binder uses this property to avoid repeated binding.
        /// </summary>
        ProductVariantQuery Current { get; }

        /// <summary>
        /// Creates a <see cref="ProductVariantQuery"/> instance from the current <see cref="IHttpContextAccessor.HttpContext"/> 
        /// by looking up corresponding keys in posted form and/or query string.
        /// </summary>
        /// <returns>Product variant query.</returns>
        ProductVariantQuery CreateFromQuery();
    }
}
