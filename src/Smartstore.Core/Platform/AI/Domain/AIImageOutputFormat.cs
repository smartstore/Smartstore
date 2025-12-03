#nullable enable

using System.ComponentModel;
using System.Net.Mime;
using Smartstore.ComponentModel;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Represents an image output format supported by AI image generation operations, such as PNG, JPEG, or WebP.
    /// </summary>
    /// <remarks>Use the predefined static fields to specify a supported format when requesting image output.
    /// This type provides implicit conversions to and from string values representing the format name. Only the formats
    /// defined by the static fields are supported; attempting to convert an unknown string will result in an exception.
    /// AIImageOutputFormat is intended for use as a type-safe alternative to raw string format identifiers.</remarks>
    [TypeConverter(typeof(StringBackedTypeConverter<AIImageOutputFormat>))]
    public readonly partial struct AIImageOutputFormat : IStringBacked<AIImageOutputFormat>, IEquatable<AIImageOutputFormat>
    {
        private readonly string _value;
        
        internal AIImageOutputFormat(string value, string mimeType) 
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
            MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        }

        /// <summary>
        /// Gets the string value represented by this instance.
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// Gets the MIME type associated with the content.
        /// </summary>
        public string MimeType { get; }

        /// <summary>
        /// Represents the PNG image output format for AI image generation.
        /// </summary>
        public static readonly AIImageOutputFormat Png = new("png", MediaTypeNames.Image.Png);

        /// <summary>
        /// Represents the JPEG image output format for AI image generation.
        /// </summary>
        public static readonly AIImageOutputFormat Jpeg = new("jpeg", MediaTypeNames.Image.Jpeg);


        /// <summary>
        /// Represents the WEBP image output format for AI image generation.
        /// </summary>
        public static readonly AIImageOutputFormat WebP = new("webp", MediaTypeNames.Image.Webp);

        /// <summary>
        /// Represents a collection of all supported image resolutions.
        /// </summary>
        public static readonly AIImageOutputFormat[] All = [Png, Jpeg, WebP];

        public static implicit operator string?(AIImageOutputFormat obj)
            => obj._value;

        public static implicit operator AIImageOutputFormat?(string? value)
            => FromString(value);

        public static AIImageOutputFormat? FromString(string? value)
        {
            if (value == null) return null;
            return value switch
            {
                "png" => Png,
                "jpeg" => Jpeg,
                "webp" => WebP,
                _ => throw new InvalidCastException($"Unknown image output format '{value}'."),
            };
        }

        public static bool operator ==(AIImageOutputFormat left, AIImageOutputFormat right) 
            => left.Equals(right);

        public static bool operator !=(AIImageOutputFormat left, AIImageOutputFormat right) 
            => !left.Equals(right);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) 
            => obj is AIImageOutputFormat other && Equals(other);

        public bool Equals(AIImageOutputFormat other) 
            => _value?.EqualsNoCase(other._value) ?? false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() 
            => _value?.GetHashCode() ?? 0;

        public override string? ToString() 
            => _value;
    }
}
