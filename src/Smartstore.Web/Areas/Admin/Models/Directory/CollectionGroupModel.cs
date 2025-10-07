using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;

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

        [LocalizedDisplay("Admin.Common.Entity")]
        public string EntityName { get; set; }

        //[LocalizedDisplay("Admin.Common.EntityId")]
        //public int EntityId { get; set; }

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

    public partial class CollectionGroupValidator : AbstractValidator<CollectionGroupModel>
    {
        public CollectionGroupValidator()
        {
            RuleFor(x => x.Name).NotEmpty().Length(1, 400);
            RuleFor(x => x.EntityName).NotEmpty().Length(1, 100);
        }
    }


    public class CollectionGroupMapper :
        IMapper<CollectionGroupModel, CollectionGroup>,
        IMapper<CollectionGroup, CollectionGroupModel>
    {
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILocalizationService _localizationService;

        public CollectionGroupMapper(ILocalizedEntityService localizedEntityService,
            ILocalizationService localizationService)
        {
            _localizedEntityService = localizedEntityService;
            _localizationService = localizationService;
        }

        public Task MapAsync(CollectionGroup from, CollectionGroupModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);

            to.LocalizedEntityName = _localizationService.GetResource("Common.Entity." + from.EntityName, 0, false, string.Empty, true);

            return Task.CompletedTask;
        }

        public async Task MapAsync(CollectionGroupModel from, CollectionGroup to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);

            foreach (var localized in from.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(to, x => x.Name, localized.Name, localized.LanguageId);
            }
        }
    }
}
