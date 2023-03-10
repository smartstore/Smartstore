using System.Globalization;

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

        string GetTwoLetterISOLanguageName();
    }

    /// <summary>
    /// A minimum, cacheable language entity.
    /// </summary>
    public class LanguageInfo : ILanguage
    {
        /// <inheritdoc />
        public int Id { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public string LanguageCulture { get; set; }

        /// <inheritdoc />
        public string UniqueSeoCode { get; set; }

        /// <inheritdoc />
        public bool Rtl { get; set; }

        /// <inheritdoc />
        public string GetTwoLetterISOLanguageName()
        {
            if (UniqueSeoCode.HasValue())
            {
                return UniqueSeoCode;
            }

            try
            {
                return new CultureInfo(LanguageCulture).TwoLetterISOLanguageName;
            }
            catch
            {
                return null;
            }
        }

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
}
