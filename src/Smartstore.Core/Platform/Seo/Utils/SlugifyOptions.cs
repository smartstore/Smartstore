namespace Smartstore.Core.Seo
{
    public class SlugifyOptions
    {
        /// <summary>
        /// Whether to remove diacritic (e.g. ç --> c). Default = true.
        /// </summary>
        public bool RemoveDiacritic { get; set; } = true;

        /// <summary>
        /// Whether unicode chars are allowed. Default = false.
        /// </summary>
        public bool AllowUnicodeChars { get; set; }

        /// <summary>
        /// Whether forward slash (/) is allowed (but only if prev or next char is not whitespace). Default = false.
        /// </summary>
        public bool AllowForwardSlash { get; set; }

        /// <summary>
        /// Whether space is allowed. Default = false.
        /// </summary>
        public bool AllowSpace { get; set; }

        /// <summary>
        /// Whether to collapse whitespace. Has no effect if <see cref="AllowSpace"/> is <c>false</c>. Default = true.
        /// </summary>
        public bool CollapseWhiteSpace { get; set; } = true;

        /// <summary>
        /// Whether to lowercase all chars. Default = true.
        /// </summary>
        public bool ForceLowerCase { get; set; } = true;

        /// <summary>
        /// Other whitelisted chars that are allowed. Optional.
        /// </summary>
        public char[] AllowedChars { get; set; }

        /// <summary>
        /// Optional character conversion map (e.g. ä --> ae).
        /// </summary>
        public IReadOnlyDictionary<char, string> CharConversionMap { get; set; }
    }
}
