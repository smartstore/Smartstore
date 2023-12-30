using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Smartstore.Collections;
using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media.Imaging
{
    public class ProcessImageQuery : MutableQueryCollection
    {
        private readonly static string[] _validScaleModes
            = ["max", "boxpad", "crop", "min", "pad", "stretch"];

        private readonly static string[] _validAnchorPositions
            = ["center", "top", "bottom", "left", "right", "top-left", "top-right", "bottom-left", "bottom-right"];

        // Key = Supported token name, Value = Validator
        private readonly static FrozenDictionary<string, Func<string, string, bool>> _supportedTokens = new Dictionary<string, Func<string, string, bool>>()
        {
            ["w"] = ValidateSizeToken,
            ["h"] = ValidateSizeToken,
            ["size"] = ValidateSizeToken,
            ["q"] = ValidateQualityToken,
            ["m"] = ValidateScaleModeToken,
            ["pos"] = ValidateAnchorPosToken
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
            get => Get<string>("bg");
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
            // Keep away invalid tokens from underlying query
            if (name != null && _supportedTokens.TryGetValue(name, out var validator) && validator(name, value))
            {
                return base.Add(name, value, isUnique);
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
            var hash = string.Empty;

            foreach (var key in this.Keys)
            {
                if (key == "m" && this["m"] == "max")
                    continue; // Mode 'max' is default and can be omitted

                hash += "-" + key + this[key];
            }

            return hash;
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

        private static bool ValidateSizeToken(string key, string value)
        {
            return uint.TryParse(value, out var size) && size < 10000;
        }

        private static bool ValidateQualityToken(string key, string value)
        {
            return uint.TryParse(value, out var q) && q <= 100;
        }

        private static bool ValidateScaleModeToken(string key, string value)
        {
            return _validScaleModes.Contains(value);
        }

        private static bool ValidateAnchorPosToken(string key, string value)
        {
            return _validAnchorPositions.Contains(value);
        }

        #endregion
    }
}
