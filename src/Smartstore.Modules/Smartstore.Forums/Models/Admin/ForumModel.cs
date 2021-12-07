using Smartstore.ComponentModel;
using Smartstore.Core.Seo;

namespace Smartstore.Forums.Models
{
    [LocalizedDisplay("Admin.ContentManagement.Forums.Forum.Fields.")]
    public class ForumModel : EntityModelBase, ILocalizedModel<ForumLocalizedModel>
    {
        [LocalizedDisplay("*ForumGroupId")]
        public int ForumGroupId { get; set; }

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
        [LocalizedDisplay("Common.CreatedOn")]
        public string CreatedOnStr
            => CreatedOn.ToString();

        public string EditUrl { get; set; }
        public List<ForumLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.ContentManagement.Forums.Forum.Fields.")]
    public class ForumLocalizedModel : ILocalizedLocaleModel
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

    public partial class ForumValidator : AbstractValidator<ForumModel>
    {
        public ForumValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.ForumGroupId).NotEmpty();
        }
    }

    public class ForumMapper :
        IMapper<Forum, ForumModel>
    {
        public async Task MapAsync(Forum from, ForumModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.SeName = await from.GetActiveSlugAsync(0, true, false);
        }
    }
}
