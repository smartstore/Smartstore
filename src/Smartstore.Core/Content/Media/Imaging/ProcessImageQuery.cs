using System.Collections.Frozen;
using System.Drawing;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Smartstore.Collections;
using Smartstore.Imaging;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media.Imaging
{
    public class ProcessImageQuery : MutableQueryCollection
    {
        private delegate bool TokenValidator(string key, string value, out string normalizedValue);

        private readonly static string[] _validScaleModes
            = ["max", "boxpad", "crop", "min", "pad", "stretch"];

        private readonly static string[] _validAnchorPositions
            = ["center", "top", "bottom", "left", "right", "top-left", "top-right", "bottom-left", "bottom-right"];

        // Key = Supported token name, Value = Validator
        private readonly static FrozenDictionary<string, TokenValidator> _supportedTokens = new Dictionary<string, TokenValidator>()
        {
            ["w"] = ValidateSizeToken,
            ["h"] = ValidateSizeToken,
            ["size"] = ValidateSizeToken,
            ["q"] = ValidateQualityToken,
            ["m"] = ValidateScaleModeToken,
            ["pos"] = ValidateAnchorPosToken,
            ["bg"] = ValidateBackgroundToken
        }.ToFrozenDictionary();

        public ProcessImageQuery()
            : this(null, null)
        {
        }

        public ProcessImageQuery(byte[] source)
            : this(source, null)
        {
        }

        public ProcessImageQuery(Stream source)
            : this(source, null)
        {
        }

        public ProcessImageQuery(string source)
            : this(source, null)
        {
        }

        public ProcessImageQuery(object source, IQueryCollection query)
        {
            Source = source;
            DisposeSource = true;
            Notify = true;

            // Add tokens sanitized
            if (query != null)
            {
                query.Keys.Each(key => Add(key, query[key], false));
            }
        }

        public ProcessImageQuery(ProcessImageQuery query)
            : base(CopyStore(query.Store))
        {
            Guard.NotNull(query);

            Source = query.Source;
            Format = query.Format;
            DisposeSource = query.DisposeSource;
        }

        private static Dictionary<string, StringValues> CopyStore(Dictionary<string, StringValues> source)
            => new(source);

        /// <summary>
        /// The source image's physical path, app-relative virtual path, or a Stream, byte array, Image or IFile instance.
        /// </summary>
        public object Source { get; set; }

        public string FileName { get; set; }

        /// <summary>
        /// Whether to dispose the source stream after processing completes
        /// </summary>
        public bool DisposeSource { get; set; }

        /// <summary>
        /// Whether to execute an applicable post processor which
        /// can reduce the resulting file size drastically, but also
        /// can slow down processing time.
        /// </summary>
        public bool ExecutePostProcessor { get; set; } = true;

        public int? MaxWidth
        {
            get => Get<int?>("w") ?? Get<int?>("size");
            set => Set("w", value);
        }

        public int? MaxHeight
        {
            get => Get<int?>("h") ?? Get<int?>("size");
            set => Set("h", value);
        }

        public int? MaxSize
        {
            get => Get<int?>("size");
            set => Set("size", value);
        }

        public int? Quality
        {
            get => Get<int?>("q");
            set => Set("q", value);
        }

        /// <summary>
        /// max (default) | boxpad | crop | min | pad | stretch
        /// </summary>
        public string ScaleMode
        {
            get => Get<string>("m");
            set => Set("m", value);
        }

        /// <summary>
        /// center (default) | top | bottom | left | right | top-left | top-right | bottom-left | bottom-right
        /// </summary>
        public string AnchorPosition
        {
            get => Get<string>("pos");
            set => Set("pos", value);
        }

        public string BackgroundColor
        {
            // canonical value is stored during Add(...); still tolerate legacy / direct Store injection
            get => TryNormalizeBackgroundColor(this["bg"], out var normalized) ? normalized : Get<string>("bg");
            set => Set("bg", value);
        }

        /// <summary>
        /// Gets or sets the output file format either as a string ("png", "jpg", "gif" and "svg"),
        /// or as a format object instance.
        /// When format is not specified, the original format of the source image is used (unless it is not a web safe format - jpeg is the fallback in that scenario).
        /// </summary>
        public object Format { get; set; }

        public bool IsValidationMode { get; set; }

        public bool Notify { get; set; }

        public override MutableQueryCollection Add(string name, string value, bool isUnique)
        {
            // Keep away invalid tokens from underlying query store and canonicalize values where applicable.
            if (name != null && _supportedTokens.TryGetValue(name, out var validator))
            {
                if (validator(name, value, out var normalized))
                {
                    // One value per token. (bg must be unique; others are effectively unique too.)
                    return base.Add(name, normalized ?? value, isUnique: true);
                }
            }

            return this;
        }

        private T Get<T>(string name)
        {
            return this[name].ToString().Convert<T>();
        }

        private void Set<T>(string name, T val)
        {
            if (val == null)
                Remove(name);
            else
                Add(name, val.Convert<string>(), true);
        }


        public bool NeedsProcessing(bool ignoreQualityFlag = false)
        {
            if (Count == 0)
                return false;

            if (ignoreQualityFlag && Count == 1 && this["q"].Count > 0)
            {
                // Return false if ignoreQualityFlag is true and "q" is the only flag.
                return false;
            }

            if (Equals(Format, "svg"))
            {
                // SVG cannot be processed.
                return false;
            }

            if (Count == 1 && Quality >= 90)
            {
                // If "q" is the only flag and its value is >= 90, we don't need to process
                return false;
            }

            return true;
        }

        public string CreateHash()
        {
            // Readable + deterministic + robust + filename-safe.
            // Also re-validates/canonicalizes as "defense in depth" for cases where Store was populated elsewhere.
            if (Count == 0)
                return string.Empty;

            var sb = new StringBuilder(64);

            foreach (var key in Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                if (!_supportedTokens.TryGetValue(key, out var validator))
                    continue;

                if (key == "m" && this["m"] == "max")
                    continue;

                var values = this[key];
                if (values.Count == 0)
                    continue;

                // Exactly one value is expected (we enforce it during Add), but stay tolerant.
                var v = values[0];
                if (v is null)
                    continue;

                if (!validator(key, v, out var normalized))
                    continue;

                sb.Append('-').Append(key).Append(normalized ?? v);
            }

            if (sb.Length == 0)
                return string.Empty;

            return PathUtility.SanitizeFileName(sb.ToString(), "_");
        }

        public string GetResultExtension()
        {
            if (Format == null)
            {
                return null;
            }
            else if (Format is IImageFormat imageFormat)
            {
                return imageFormat.DefaultExtension;
            }
            else if (Format is string str)
            {
                return str;
            }

            return null;
        }

        #region Static Helpers

        public static ResizeMode ConvertScaleMode(string mode)
        {
            switch (mode.EmptyNull().ToLower())
            {
                case "boxpad":
                    return ResizeMode.BoxPad;
                case "crop":
                    return ResizeMode.Crop;
                case "min":
                    return ResizeMode.Min;
                case "pad":
                    return ResizeMode.Pad;
                case "stretch":
                    return ResizeMode.Stretch;
                default:
                    return ResizeMode.Max;
            }
        }

        public static AnchorPosition ConvertAnchorPosition(string anchor)
        {
            switch (anchor.EmptyNull().ToLower())
            {
                case "top":
                    return Smartstore.Imaging.AnchorPosition.Top;
                case "bottom":
                    return Smartstore.Imaging.AnchorPosition.Bottom;
                case "left":
                    return Smartstore.Imaging.AnchorPosition.Left;
                case "right":
                    return Smartstore.Imaging.AnchorPosition.Right;
                case "top-left":
                    return Smartstore.Imaging.AnchorPosition.TopLeft;
                case "top-right":
                    return Smartstore.Imaging.AnchorPosition.TopRight;
                case "bottom-left":
                    return Smartstore.Imaging.AnchorPosition.BottomLeft;
                case "bottom-right":
                    return Smartstore.Imaging.AnchorPosition.BottomRight;
                default:
                    return Smartstore.Imaging.AnchorPosition.Center;
            }
        }

        private static bool ValidateSizeToken(string key, string value, out string normalizedValue)
        {
            normalizedValue = null;
            return uint.TryParse(value, out var size) && size < 10000;
        }

        private static bool ValidateQualityToken(string key, string value, out string normalizedValue)
        {
            normalizedValue = null;
            return uint.TryParse(value, out var q) && q <= 100;
        }

        private static bool ValidateScaleModeToken(string key, string value, out string normalizedValue)
        {
            normalizedValue = null;
            return _validScaleModes.Contains(value);
        }

        private static bool ValidateAnchorPosToken(string key, string value, out string normalizedValue)
        {
            normalizedValue = null;
            return _validAnchorPositions.Contains(value);
        }

        private static bool ValidateBackgroundToken(string key, string value, out string normalizedValue)
        {
            // IMPORTANT (URL semantics):
            // A raw '#' starts the URL fragment and is NOT sent to the server.
            // Callers MUST URL-encode it: bg=%23ff0000 instead of bg=#ff0000.
            //
            // Canonicalization:
            // - 'ff0000' / '#ff0000' => '#ff0000'
            // - 'fff'    / '#fff'    => '#fff'
            // - named colors => trimmed as-is
            normalizedValue = null;

            if (!TryNormalizeBackgroundColor(value, out var normalized))
                return false;

            // Return canonical representation so it gets stored and used by the processor.
            normalizedValue = normalized;

            try
            {
                _ = ColorTranslator.FromHtml(normalizedValue);
                return true;
            }
            catch
            {
                normalizedValue = null;
                return false;
            }
        }

        private static bool TryNormalizeBackgroundColor(StringValues values, out string normalized)
        {
            normalized = null;

            if (values.Count == 0)
                return false;

            // Only one bg allowed. If multiple exist, ignore excess deterministically (first wins).
            return TryNormalizeBackgroundColor(values[0], out normalized);
        }

        private static bool TryNormalizeBackgroundColor(string value, out string normalized)
        {
            normalized = null;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var s = value.AsSpan().Trim();

            // Optional leading '#'
            var candidate = s;
            if (candidate[0] == '#')
                candidate = candidate[1..];

            // Hex (#RGB or #RRGGBB)
            if (candidate.Length is 3 or 6 && IsAsciiHex(candidate))
            {
                normalized = "#" + candidate.ToString();
                return true;
            }

            // Named colors (letters only)
            if (IsAsciiLettersOnly(s))
            {
                normalized = s.ToString();
                return true;
            }

            return false;

            static bool IsAsciiHex(ReadOnlySpan<char> span)
            {
                foreach (var c in span)
                {
                    var isDigit = (uint)(c - '0') <= 9;
                    var isLower = (uint)(c - 'a') <= 5;
                    var isUpper = (uint)(c - 'A') <= 5;

                    if (!isDigit && !isLower && !isUpper)
                        return false;
                }

                return true;
            }

            static bool IsAsciiLettersOnly(ReadOnlySpan<char> span)
            {
                if (span.Length == 0)
                    return false;

                foreach (var c in span)
                {
                    var isLower = (uint)(c - 'a') <= 25;
                    var isUpper = (uint)(c - 'A') <= 25;

                    if (!isLower && !isUpper)
                        return false;
                }

                return true;
            }
        }

        #endregion
    }
}
