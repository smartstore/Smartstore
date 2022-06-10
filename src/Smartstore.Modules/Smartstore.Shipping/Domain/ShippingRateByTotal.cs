using System.ComponentModel.DataAnnotations.Schema;
using Smartstore.Domain;

namespace Smartstore.Shipping.Domain
{
    /// <summary>
    /// Represents a Shipping rate.
    /// </summary>
    [Table("ShippingByTotal")]
    public partial class ShippingRateByTotal : BaseEntity
    {
        /// <summary>
        /// Gets or sets the shipping method identifier.
        /// </summary>
        public int ShippingMethodId { get; set; }

        /// <summary>
        /// Gets or sets the store identifier
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Gets or sets the country identifier
        /// </summary>
        public int? CountryId { get; set; }

        /// <summary>
        /// Gets or sets the state/province identifier
        /// </summary>
        public int? StateProvinceId { get; set; }

        /// <summary>
        /// Gets or sets the zip code.
        /// </summary>
        [StringLength(100)]
        public string Zip { get; set; }

        /// <summary>
        /// Gets or sets the "from" value.
        /// </summary>
        public decimal From { get; set; }

        /// <summary>
        /// Gets or sets the "to" value.
        /// </summary>
        public decimal? To { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use percentage.
        /// </summary>
        public bool UsePercentage { get; set; }

        /// <summary>
        /// Gets or sets the shipping charge percentage.
        /// </summary>
        public decimal ShippingChargePercentage { get; set; }

        /// <summary>
        /// Gets or sets the shipping charge amount.
        /// </summary>
        public decimal ShippingChargeAmount { get; set; }

        /// <summary>
        /// Gets or sets the base shipping charge (if <see cref="UsePercentage"/> is set to <c>true</c>).
        /// </summary>
        public decimal BaseCharge { get; set; }

        /// <summary>
        /// Gets or sets the max shipping charge (if <see cref="UsePercentage"/> is set to <c>true</c>).
        /// </summary>
        public decimal? MaxCharge { get; set; }
    }
}
