using FluentValidation;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.Copy.")]
    public class CopyProductModel : EntityModelBase
    {
        [LocalizedDisplay("*NumberOfCopies")]
        public int NumberOfCopies { get; set; } = 1;

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Published")]
        public bool Published { get; set; }
    }

    public partial class CopyProductValidator : AbstractValidator<CopyProductModel>
    {
        public CopyProductValidator()
        {
            RuleFor(x => x.NumberOfCopies).NotEmpty().GreaterThan(0);
        }
    }
}
