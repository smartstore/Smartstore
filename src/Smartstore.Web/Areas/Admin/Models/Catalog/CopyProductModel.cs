using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Copy.")]
    public class CopyProductModel : EntityModelBase
    {
        [AdditionalMetadata("min", 1)]
        [AdditionalMetadata("max", 100)]
        [LocalizedDisplay("*NumberOfCopies")]
        public int NumberOfCopies { get; set; } = 1;

        [Required]
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }
    }
}
