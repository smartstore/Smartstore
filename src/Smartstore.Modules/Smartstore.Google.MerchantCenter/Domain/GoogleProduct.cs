using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Smartstore.Domain;

namespace Smartstore.Google.MerchantCenter.Domain
{
    [Table("GoogleProduct")]
    [Index(nameof(ProductId))]
    [Index(nameof(IsTouched))]
    [Index(nameof(Export))]
    public class GoogleProduct : BaseEntity
    {
        public int ProductId { get; set; }

        [StringLength(4000)]
        public string Taxonomy { get; set; }

        [StringLength(100)]
        public string Gender { get; set; }

        [StringLength(100)]
        public string AgeGroup { get; set; }

        [StringLength(100)]
        public string Color { get; set; }

        [StringLength(100)]
        public string Size { get; set; }

        [StringLength(400)]
        public string Material { get; set; }

        [StringLength(400)]
        public string Pattern { get; set; }

        [StringLength(4000)]
        public string ItemGroupId { get; set; }
        public bool IsTouched { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public bool Export { get; set; } = true;
        public int Multipack { get; set; }
        public bool? IsBundle { get; set; }
        public bool? IsAdult { get; set; }

        [StringLength(50)]
        public string EnergyEfficiencyClass { get; set; }

        [StringLength(1000)]
        public string MediaFileIds { get; set; }

        [StringLength(100)]
        public string CustomLabel0 { get; set; }

        [StringLength(100)]
        public string CustomLabel1 { get; set; }

        [StringLength(100)]
        public string CustomLabel2 { get; set; }

        [StringLength(100)]
        public string CustomLabel3 { get; set; }

        [StringLength(100)]
        public string CustomLabel4 { get; set; }

        public bool IsDefault()
        {
            return string.IsNullOrEmpty(Taxonomy)
                && string.IsNullOrEmpty(Gender)
                && string.IsNullOrEmpty(AgeGroup)
                && string.IsNullOrEmpty(Color)
                && string.IsNullOrEmpty(Size)
                && string.IsNullOrEmpty(Material)
                && string.IsNullOrEmpty(Pattern)
                && string.IsNullOrEmpty(ItemGroupId)
                && Export
                && Multipack == 0
                && IsBundle == null
                && IsAdult == null
                && string.IsNullOrEmpty(EnergyEfficiencyClass)
                && string.IsNullOrEmpty(MediaFileIds)
                && string.IsNullOrEmpty(CustomLabel0)
                && string.IsNullOrEmpty(CustomLabel1)
                && string.IsNullOrEmpty(CustomLabel2)
                && string.IsNullOrEmpty(CustomLabel3)
                && string.IsNullOrEmpty(CustomLabel4);
        }
    }
}