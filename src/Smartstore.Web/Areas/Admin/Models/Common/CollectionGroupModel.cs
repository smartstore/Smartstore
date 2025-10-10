using FluentValidation;

namespace Smartstore.Admin.Models.Common
{
    public class CollectionGroupListModel : ModelBase
    {
        [LocalizedDisplay("Admin.Common.Entity")]
        public string EntityName { get; set; }

        [LocalizedDisplay("Admin.Common.IsPublished")]
        public bool? Published { get; set; }
    }

    [LocalizedDisplay("Admin.Configuration.CollectionGroup.")]
    public class CollectionGroupModel : EntityModelBase, ILocalizedModel<CollectionGroupLocalizedModel>
    {
        public Type GetEntityType() => typeof(CollectionGroup);

        public List<CollectionGroupLocalizedModel> Locales { get; set; } = [];

        [LocalizedDisplay("*EntityName")]
        public string EntityName { get; set; }

        [LocalizedDisplay("*EntityName")]
        public string LocalizedEntityName { get; set; }

        [LocalizedDisplay("*NumberOfAssignments")]
        public int NumberOfAssignments { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Common.IsPublished")]
        public bool Published { get; set; } = true;

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }

    [LocalizedDisplay("Admin.Configuration.CollectionGroup.")]
    public class CollectionGroupLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }
    }

    public partial class CollectionGroupValidator : AbstractValidator<CollectionGroupModel>
    {
        public CollectionGroupValidator()
        {
            RuleFor(x => x.Name).NotEmpty().Length(1, 400);
            RuleFor(x => x.EntityName).NotEmpty().Length(1, 100);
        }
    }
}
