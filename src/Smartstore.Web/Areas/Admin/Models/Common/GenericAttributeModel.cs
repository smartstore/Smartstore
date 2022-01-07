using FluentValidation;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Common.GenericAttributes.Fields.")]
    public partial class GenericAttributeModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Key { get; set; }

        [LocalizedDisplay("*Value")]
        public string Value { get; set; }

        public string EntityName { get; set; }

        public int AttributeEntityId { get; set; }
    }

    public partial class GenericAttributeValidator : AbstractValidator<GenericAttributeModel>
    {
        public GenericAttributeValidator()
        {
            RuleFor(x => x.Key).NotEmpty();
        }
    }
}
