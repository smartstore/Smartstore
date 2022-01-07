namespace Smartstore.Core.Localization
{
    public delegate LocalizedString Localizer(string key, params object[] args);
    public delegate LocalizedString LocalizerEx(string key, int languageId, params object[] args);

    public static class NullLocalizer
    {
        static NullLocalizer()
        {
            Instance = (format, args) => new LocalizedString(format, null, args);
            InstanceEx = (format, languageId, args) => new LocalizedString(format, null, args);
        }

        public static Localizer Instance { get; }
        public static LocalizerEx InstanceEx { get; }
    }
}
