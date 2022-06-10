using FluentValidation;

namespace Smartstore.Admin.Models.Tax
{
    public class TaxCategoryModel : EntityModelBase
    {
        [LocalizedDisplay("Admin.Configuration.Tax.Categories.Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }

    public partial class TaxCategoryValidator : AbstractValidator<TaxCategoryModel>
    {
        public TaxCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
