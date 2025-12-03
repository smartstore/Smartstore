#nullable enable

using System.ComponentModel;
using Smartstore.ComponentModel;

namespace Smartstore.Core.AI
{
    /// <summary>
    /// Represents a predefined image resolution for AI-generated images, such as HD (1K), QHD (2K), or UHD (4K).
    /// </summary>
    /// <remarks>
    /// Use the static fields to specify common resolutions when working with AI image generation
    /// APIs. This struct provides value-based equality and can be compared using the equality operators. The string
    /// representation of the resolution can be obtained using the ToString method.
    /// </remarks>
    [TypeConverter(typeof(StringBackedTypeConverter<AIImageResolution>))]
    public readonly partial struct AIImageResolution : IStringBacked<AIImageResolution>, IEquatable<AIImageResolution>
    {
        private readonly string _value;
        
        internal AIImageResolution(string value) 
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the string value represented by this instance.
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// Represents the HD (1K) image resolution preset for AI image generation.
        /// </summary>
        public static readonly AIImageResolution HD = new("1K");

        /// <summary>
        /// Represents the QHD (2K) image resolution preset for AI image generation.
        /// </summary>
        public static readonly AIImageResolution QHD = new("2K");

        /// <summary>
        /// Represents the UHD (4K) image resolution preset for AI image generation.
        /// </summary>
        public static readonly AIImageResolution UHD = new("4K");

        /// <summary>
        /// Represents a collection of all supported image resolutions.
        /// </summary>
        public static readonly AIImageResolution[] All = [HD, QHD, UHD];

        public static implicit operator string?(AIImageResolution obj)
            => obj._value;

        public static implicit operator AIImageResolution?(string? value)
            => FromString(value);

        public static AIImageResolution? FromString(string? value)
        {
            if (value == null) return null;
            return value switch
            {
                "1K" => HD,
                "2K" => QHD,
                "4K" => UHD,
                _ => throw new InvalidCastException($"Unknown image resolution '{value}'."),
            };
        }

        public static bool operator ==(AIImageResolution left, AIImageResolution right) 
            => left.Equals(right);

        public static bool operator !=(AIImageResolution left, AIImageResolution right) 
            => !left.Equals(right);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) 
            => obj is AIImageResolution other && Equals(other);

        public bool Equals(AIImageResolution other) 
            => _value?.EqualsNoCase(other._value) ?? false;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() 
            => _value?.GetHashCode() ?? 0;

        public override string? ToString() 
            => _value;
    }
}
