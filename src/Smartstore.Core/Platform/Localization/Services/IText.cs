namespace Smartstore.Core.Localization
{
    public partial interface IText
    {
        LocalizedString Get(string key, params object[] args);
        LocalizedString GetEx(string key, int languageId, params object[] args);
    }
}
