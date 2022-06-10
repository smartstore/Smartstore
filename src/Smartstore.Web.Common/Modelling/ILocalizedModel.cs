namespace Smartstore.Web.Modelling
{
    public interface ILocalizedModel
    {
    }

    public interface ILocalizedModel<T> : ILocalizedModel where T : ILocalizedLocaleModel
    {
        List<T> Locales { get; set; }
    }

    public interface ILocalizedLocaleModel
    {
        int LanguageId { get; set; }
    }
}
