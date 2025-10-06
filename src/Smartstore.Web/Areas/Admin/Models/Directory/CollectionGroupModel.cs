namespace Smartstore.Admin.Models.Common
{
    public class CollectionGroupListModel : ModelBase
    {
        [LocalizedDisplay("Admin.Common.Entity")]
        public string EntityName { get; set; }

        [LocalizedDisplay("Admin.Common.EntityId")]
        public int? EntityId { get; set; }

        [LocalizedDisplay("Admin.Common.IsPublished")]
        public bool? Published { get; set; }
    }

    [LocalizedDisplay("Admin.Configuration.CollectionGroup.")]
    public class CollectionGroupModel : EntityModelBase, ILocalizedModel<CollectionGroupLocalizedModel>
    {
        public Type GetEntityType() => typeof(CollectionGroup);

        public List<CollectionGroupLocalizedModel> Locales { get; set; } = [];

        [LocalizedDisplay("Admin.Common.Entity")]
        public string EntityName { get; set; }

        [LocalizedDisplay("Admin.Common.EntityId")]
        public int EntityId { get; set; }

        [LocalizedDisplay("*EntityName")]
        public string LocalizedEntityName { get; set; }

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
}
