using System.Dynamic;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Catalog
{
    internal static partial class GroupedProductConfigurationMappingExtensions
    {
        public static Task MapAsync(this Product from, GroupedProductConfigurationModel to)
        {
            dynamic parameters = new ExpandoObject();
            parameters.Product = from;

            var mapper = MapperFactory.GetMapper<GroupedProductConfiguration, GroupedProductConfigurationModel>();

            return mapper.MapAsync(from.GroupedProductConfiguration, to, parameters);
        }

        public static async Task<GroupedProductConfiguration> MapAsync(this GroupedProductConfigurationModel from)
        {
            var to = new GroupedProductConfiguration();

            var mapper = MapperFactory.GetMapper<GroupedProductConfigurationModel, GroupedProductConfiguration>();
            await mapper.MapAsync(from, to);

            return to;
        }
    }


    internal class GroupedProductConfigurationMapper :
        IMapper<GroupedProductConfiguration, GroupedProductConfigurationModel>,
        IMapper<GroupedProductConfigurationModel, GroupedProductConfiguration>
    {
        private readonly ILanguageService _languageService;
        private readonly CatalogSettings _catalogSettings;

        public GroupedProductConfigurationMapper(
            ILanguageService languageService,
            CatalogSettings catalogSettings)
        {
            _languageService = languageService;
            _catalogSettings = catalogSettings;
        }

        public async Task MapAsync(GroupedProductConfiguration from, GroupedProductConfigurationModel to, dynamic parameters = null)
        {
            Guard.NotNull(to);

            var product = Guard.NotNull(parameters.Product as Product);
            var allLanguages = await _languageService.GetAllLanguagesAsync(true);
            var languageMap = allLanguages.ToDictionary(x => x.Id);

            if (from != null)
            {
                MiniMapper.Map(from, to);
            }

            to.Id = product.Id;
            to.Title = from?.Titles?.Get(string.Empty);
            to.DefaultTitle = _catalogSettings.AssociatedProductsTitle;

            foreach (var language in allLanguages)
            {
                to.Locales.Add(new()
                {
                    LanguageId = language.Id,
                    Title = from?.Titles?.Get(languageMap.Get(language.Id).LanguageCulture),
                    DefaultTitle = _catalogSettings.GetLocalizedSetting(x => x.AssociatedProductsTitle, language.Id, null, false, false)
                });
            }
        }

        public async Task MapAsync(GroupedProductConfigurationModel from, GroupedProductConfiguration to, dynamic parameters = null)
        {
            Guard.NotNull(from);

            var allLanguages = await _languageService.GetAllLanguagesAsync(true);
            var languageMap = allLanguages.ToDictionary(x => x.Id);

            MiniMapper.Map(from, to);

            to.Titles = new()
            {
                [string.Empty] = from.Title
            };

            foreach (var localized in from.Locales)
            {
                to.Titles[languageMap.Get(localized.LanguageId).LanguageCulture] = localized.Title;
            }
        }
    }
}
