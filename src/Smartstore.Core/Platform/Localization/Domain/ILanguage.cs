namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Represents a language entity
    /// </summary>
    public interface ILanguage
    {
        /// <summary>
        /// Entity id.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Language display name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Language culture (e.g. "en-US")
        /// </summary>
        string LanguageCulture { get; }

        /// <summary>
        /// The unique SEO code (e.g. "en")
        /// </summary>
        string UniqueSeoCode { get; }

        /// <summary>
        /// Whether language is "Right-to-left"
        /// </summary>
        bool Rtl { get; }
    }

    /// <summary>
    /// A minimum, cacheable language entity.
    /// </summary>
    public class LanguageInfo : ILanguage
    {
        public LanguageInfo()
        {
        }

        public LanguageInfo(ILanguage language)
        {
            Id = language.Id;
            Name = language.Name;
            LanguageCulture = language.LanguageCulture;
            UniqueSeoCode = language.UniqueSeoCode;
            Rtl = language.Rtl;
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public string LanguageCulture { get; set; }
        public string UniqueSeoCode { get; set; }

        public bool Rtl { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as LanguageInfo;

            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            if (Id == 0)
            {
                return base.GetHashCode();
            }
            else
            {
                return HashCode.Combine(typeof(LanguageInfo), Id);
            }
        }

        public static bool operator ==(LanguageInfo x, LanguageInfo y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(LanguageInfo x, LanguageInfo y)
        {
            return !Equals(x, y);
        }
    }

    public class ExtendedLanguageInfo : LanguageInfo
    {
        public string FlagImageFileName { get; set; }
        public string ShortName { get; set; }
        public string LocalizedName { get; set; }
        public string LocalizedShortName { get; set; }
    }
}
