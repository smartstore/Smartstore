using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents a pricable item.
    /// </summary>
    public interface IPricable
    {
        /// <summary>
        /// Gets the pricable item unique identifier.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the product regular price.
        /// </summary>
        Task<decimal> GetRegularPriceAsync();

        /// <summary>
        /// Gets the tax category identifier.
        /// </summary>
        int TaxCategoryId { get; }

        /// <summary>
        /// Gets a value indicating whether the product is marked as tax exempt.
        /// </summary>
        bool IsTaxExempt { get; }

        /// <summary>
        /// Gets a value indicating whether the product is an electronic service
        /// bound to EU VAT regulations for digital goods.
        /// </summary>
        bool IsEsd { get; }
    }
}
