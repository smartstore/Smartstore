using System.ComponentModel.DataAnnotations.Schema;
using Smartstore.Domain;

namespace Smartstore.Tax.Domain
{
    /// <summary>
    /// Represents a tax rate.
    /// </summary>
    [Table("TaxRate")]
    public partial class TaxRateEntity : BaseEntity
    {
        /// <summary>
        /// Gets or sets the tax category identifier.
        /// </summary>
        public int TaxCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the country identifier.
        /// </summary>
        public int CountryId { get; set; }

        /// <summary>
        /// Gets or sets the state/province identifier.
        /// </summary>
        public int StateProvinceId { get; set; }

        /// <summary>
        /// Gets or sets the zip code.
        /// </summary>
        [StringLength(100)]
        public string Zip { get; set; }

        /// <summary>
        /// Gets or sets the percentage.
        /// </summary>
        public decimal Percentage { get; set; }
    }
}
