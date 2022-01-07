namespace Smartstore.Web.Modelling
{
    public interface ISeoModel : ILocalizedModel<SeoModelLocal>
    {
        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        string MetaTitle { get; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        string MetaDescription { get; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        string MetaKeywords { get; }
    }

    public class SeoModel : ISeoModel
    {
        public string MetaTitle { get; set; }

        public string MetaDescription { get; set; }

        public string MetaKeywords { get; set; }

        public List<SeoModelLocal> Locales { get; set; } = new();
    }

    public class SeoModelLocal : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaTitle")]
        public string MetaTitle { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaDescription")]
        public string MetaDescription { get; set; }

        [LocalizedDisplay("Admin.Configuration.Seo.MetaKeywords")]
        public string MetaKeywords { get; set; }
    }
}
