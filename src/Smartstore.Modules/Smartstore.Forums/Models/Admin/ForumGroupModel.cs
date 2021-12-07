using Smartstore.ComponentModel;
using Smartstore.Core.Seo;

namespace Smartstore.Forums.Models
{
    [LocalizedDisplay("Plugins.Smartstore.Forums.List.")]
    public class ForumGroupListModel
    {
        [LocalizedDisplay("*SearchName")]
        public string SearchName { get; set; }

        [UIHint("Stores")]
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }
    }

    [LocalizedDisplay("Admin.ContentManagement.Forums.ForumGroup.Fields.")]
    public class ForumGroupModel : EntityModelBase, ILocalizedModel<ForumGroupLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 4)]
        [LocalizedDisplay("*Description")]
        public string Description { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        // Store mapping.
        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        // ACL.
        [UIHint("CustomerRoles")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public int[] SelectedCustomerRoleIds { get; set; }

        [LocalizedDisplay("Admin.Common.CustomerRole.LimitedTo")]
        public bool SubjectToAcl { get; set; }

        public string EditUrl { get; set; }
        public List<ForumGroupLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.ContentManagement.Forums.ForumGroup.Fields.")]
    public class ForumGroupLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.SeName")]
        public string SeName { get; set; }

        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 4)]
        [LocalizedDisplay("*Description")]
        public string Description { get; set; }
    }

    public partial class ForumGroupValidator : AbstractValidator<ForumGroupModel>
    {
        public ForumGroupValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    public class ForumGroupMapper :
        IMapper<ForumGroup, ForumGroupModel>
    {
        public async Task MapAsync(ForumGroup from, ForumGroupModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
        }
    }
}
