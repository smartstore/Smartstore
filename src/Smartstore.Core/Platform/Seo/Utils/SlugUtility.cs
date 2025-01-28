using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Autofac;

namespace Smartstore.Core.Seo
{
    public static partial class SlugUtility
    {
        const int SlugMaxLength = 400;

        /// <inheritdoc cref="Slugify(string, SlugifyOptions)"/>
        /// <param name="seoSettings">SEO settings</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Slugify(string input, SeoSettings seoSettings)
        {
            seoSettings ??= EngineContext.Current.Application.Services.ResolveOptional<SeoSettings>() ?? new SeoSettings();
            return Slugify(input, new SlugifyOptions
            {
                RemoveDiacritic = seoSettings.ConvertNonWesternChars,
                AllowUnicodeChars = seoSettings.AllowUnicodeCharsInUrls,
                AllowForwardSlash = true,
                CharConversionMap = seoSettings.GetCharConversionMap()
            });
        }

        /// <inheritdoc cref="Slugify(string, bool, bool, bool, IReadOnlyDictionary{char, string})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Slugify(
            string input,
            bool removeDiacritic,
            bool allowUnicodeChars,
            IReadOnlyDictionary<char, string> charConversionMap = null)
        {
            return Slugify(input, new SlugifyOptions
            {
                RemoveDiacritic = removeDiacritic,
                AllowUnicodeChars = allowUnicodeChars,
                AllowForwardSlash = true,
                CharConversionMap = charConversionMap
            });
        }

        /// <inheritdoc cref="Slugify(string, SlugifyOptions)"/>
        /// <param name="removeDiacritic">Whether to remove diacritic (e.g. ç --> c).</param>
        /// <param name="allowUnicodeChars">Whether unicode chars are allowed</param>
        /// <param name="allowForwardSlash">Whether forward slash (/) is allowed (but only if prev or next char is not whitespace)</param>
        /// <param name="charConversionMap">Optional character conversion map (e.g. ä --> ae).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Slugify(
            string input,
            bool removeDiacritic,
            bool allowUnicodeChars,
            bool allowForwardSlash,
            IReadOnlyDictionary<char, string> charConversionMap = null)
        {
            return Slugify(input, new SlugifyOptions
            {
                RemoveDiacritic = removeDiacritic,
                AllowUnicodeChars = allowUnicodeChars,
                AllowForwardSlash = allowForwardSlash,
                CharConversionMap = charConversionMap
            });
        }

        /// <summary>
        /// Slugifies a given string, that is - with default settings -
        /// you will get a SEO friendly hyphenized, lowercase, alphanumeric version of the input string, 
        /// with any diacritics removed and whitespace collapsed.
        /// </summary>
        /// <example>
        /// a ambição cerra o coração  -->  a-ambicao-cerra-o-coracao
        /// </example>
        /// <param name="input">String to be slugified</param>
        /// <param name="options">The slugify options</param>
        /// <returns>SEO friendly slugified string</returns>
        public static string Slugify(string input, SlugifyOptions options = null)
        {
            // Return empty value if text is null or empty
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var len = input.Length;
            bool? dash = null;
            bool? space;

            var vsb = new ValueStringBuilder(len);

            options ??= new();
            var charConversionMap = options.CharConversionMap;

            char c;

            for (int i = 0; i < len; i++)
            {
                if (i > SlugMaxLength)
                {
                    break;
                }

                c = input[i];

                if (options.ForceLowerCase && char.IsUpper(c))
                {
                    c = char.ToLowerInvariant(c);
                }

                dash = null;
                space = null;

                if (charConversionMap != null && charConversionMap.TryGetValue(c, out var userChar))
                {
                    vsb.Append(userChar);
                    continue;
                }

                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    vsb.Append(c);
                    continue;
                }

                if (!options.ForceLowerCase && (c >= 'A' && c <= 'Z'))
                {
                    vsb.Append(c);
                    continue;
                }

                if (options.AllowedChars != null && options.AllowedChars.Contains(c))
                {
                    vsb.Append(c);
                    continue;
                }

                if (c == '/')
                {
                    if (options.AllowForwardSlash)
                    {
                        // Allow forward slash only if prev or next char is not whitespace
                        if (
                            (i > 0 && !char.IsWhiteSpace(input[i - 1])) ||
                            (i + 1 < len && !char.IsWhiteSpace(input[i + 1])))
                        {
                            vsb.Append(c);
                        }
                    }

                    continue;
                }

                if (c == ' ')
                {
                    if (options.AllowSpace)
                    {
                        if (!options.CollapseWhiteSpace || !IsPrevSpace(ref vsb))
                        {
                            vsb.Append(c);
                        }
                    }
                    else if (vsb.Length > 0 && !IsPrevDash(ref vsb))
                    {
                        vsb.Append('-');
                    }

                    continue;
                }

                if (c == ',' || c == '.' || c == '\\' || c == '-' || c == '_' || c == '=')
                {
                    if (!IsPrevDash(ref vsb))
                    {
                        vsb.Append('-');
                    }

                    continue;
                }

                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.Surrogate)
                {
                    continue;
                }
                else if (category >= UnicodeCategory.ConnectorPunctuation && category <= UnicodeCategory.MathSymbol)
                {
                    if (!IsPrevDash(ref vsb))
                    {
                        vsb.Append('-');
                    }

                    continue;
                }

                if (c >= 128)
                {
                    if (options.RemoveDiacritic && c.TryRemoveDiacritic(out var normalized))
                    {
                        vsb.Append(normalized);
                    }
                    else if (options.AllowUnicodeChars && char.IsLetterOrDigit(c))
                    {
                        vsb.Append(c);
                    }
                }
            }

            // Trim allocation-free
            if (vsb.Length > 0)
            {
                if (IsPrevDash(ref vsb))
                {
                    vsb.Slice(0, vsb.Length - 1);
                }

                if (vsb[0] == '/')
                {
                    vsb.Slice(1);
                }

                if (vsb[^1] == '/')
                {
                    vsb.Slice(0, vsb.Length - 1);
                }
            }

            return vsb.ToString();

            bool IsPrevDash(ref ValueStringBuilder vsb)
            {
                return dash ??= vsb.Length > 0 && (vsb[^1] == '-' || vsb[^1] == '_');
            }

            bool IsPrevSpace(ref ValueStringBuilder vsb)
            {
                return space ??= vsb.Length > 0 && vsb[^1] == ' ';
            }
        }
    }
}