using FluentValidation;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Admin.Models.Modularity
{
    [LocalizedDisplay("Admin.Configuration.Plugins.Fields.")]
    public class ModuleModel : ModelBase, ILocalizedModel<ModuleLocalizedModel>
    {
        [LocalizedDisplay("*Group")]
        public string Group { get; set; }

        [LocalizedDisplay("*FriendlyName")]
        public string FriendlyName { get; set; }

        [LocalizedDisplay("*SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }

        [LocalizedDisplay("*Version")]
        public string Version { get; set; }

        [LocalizedDisplay("*Author")]
        public string Author { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*Configure")]
        public string ConfigurationUrl { get; set; }

        public string Url { get; set; }

        [LocalizedDisplay("*Installed")]
        public bool Installed { get; set; }

        public LicenseLabelModel LicenseLabel { get; set; } = new();

        public bool IsConfigurable { get; set; }

        public RouteInfo ConfigurationRoute { get; set; }

        public IModuleDescriptor ModuleDescriptor { get; set; }

        /// <summary>
        /// Returns the absolute path of the provider's icon url. 
        /// </summary>
        /// <remarks>
        public string IconUrl { get; set; }

        public List<ModuleLocalizedModel> Locales { get; set; } = new();

        public int[] SelectedStoreIds { get; set; }
    }


    public class ModuleLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Common.FriendlyName")]
        public string FriendlyName { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }
    }

    public partial class ModuleValidator : AbstractValidator<ModuleModel>
    {
        public ModuleValidator()
        {
            RuleFor(x => x.FriendlyName).NotEmpty();
        }
    }
}
