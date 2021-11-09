using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Smartstore.Domain;

namespace Smartstore.Tax.Domain
{
    // TODO: (mh) (core) We can define cross-project navigation properties in Core (was impossible in Classic).
    // Please define props: TaxCategory, Country, StateProvince along with proper mapping information and migration.
    // Refactor calling code where applicable.
    
    /// <summary>
    /// Represents a tax rate.
    /// </summary>
    [Table("TaxRate")]
    public partial class TaxRateEntity : BaseEntity
    {
        /// <summary>
        /// Gets or sets the tax category identifier
        /// </summary>
        public int TaxCategoryId { get; set; }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public int CountryId { get; set; }

        /// <summary>
        /// Gets or sets the state/province identifier
        /// </summary>
        public int StateProvinceId { get; set; }

        /// <summary>
        /// Gets or sets the zip
        /// </summary>
        [StringLength(10)]
        public string Zip { get; set; }

        /// <summary>
        /// Gets or sets the percentage
        /// </summary>
        public decimal Percentage { get; set; }
    }
}
