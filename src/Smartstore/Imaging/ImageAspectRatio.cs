using System.ComponentModel;

namespace Smartstore.Imaging
{
    /// <summary>
    /// Represents a standardized image aspect ratio, such as 16:9 or 4:3, for use in image processing or display scenarios.
    /// </summary>
    /// <remarks>Use the predefined static fields to reference common aspect ratios, or implicitly convert
    /// from a string to create an instance for supported ratios. The struct supports equality comparison and can be
    /// implicitly converted to and from its string representation. Only the listed aspect ratios are supported;
    /// attempting to convert an unsupported string will result in an exception.</remarks>
    public readonly partial struct ImageAspectRatio : IEquatable<ImageAspectRatio>
    {
        private readonly string _value;

        public ImageAspectRatio(string value) 
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));

            var sides = value.Split(':').Select(x => x.ToInt()).ToArray();
            Orientation = sides[0] switch
            {
                var w when w > sides[1] => ImageOrientation.Landscape,
                var w when w < sides[1] => ImageOrientation.Portrait,
                _ => ImageOrientation.Square,
            };
        }

        /// <summary>
        /// Gets the string value represented by this instance.
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// Gets the computed orientation, e.g. landscape (16:9), portrait (9:16), or square (1:1), of the aspect ratio.
        /// </summary>
        public ImageOrientation Orientation { get; }

        /// <summary>
        /// Represents the 21:9 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio21x9 = new("21:9");

        /// <summary>
        /// Represents the 16:9 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio16x9 = new("16:9");

        /// <summary>
        /// Represents the 3:2 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio3x2 = new("3:2");

        /// <summary>
        /// Represents the 4:3 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio4x3 = new("4:3");

        /// <summary>
        /// Represents the 5:4 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio5x4 = new("5:4");

        /// <summary>
        /// Represents the 1:1 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio1x1 = new("1:1");

        /// <summary>
        /// Represents the 4:5 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio4x5 = new("4:5");

        /// <summary>
        /// Represents the 3:4 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio3x4 = new("3:4");

        /// <summary>
        /// Represents the 2:3 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio2x3 = new("2:3");

        /// <summary>
        /// Represents the 9:16 image aspect ratio.
        /// </summary>
        public static readonly ImageAspectRatio Ratio9x16 = new("9:16");

        /// <summary>
        /// Represents a collection of predefined image aspect ratios commonly used for landscape-oriented images.
        /// </summary>
        /// <remarks>The collection includes aspect ratios such as 21:9, 16:9, 3:2, 4:3, and 5:4, which
        /// are frequently used in photography, video, and display standards.</remarks>
        public static readonly ImageAspectRatio[] Landscapes = [Ratio21x9, Ratio16x9, Ratio3x2, Ratio4x3, Ratio5x4];

        /// <summary>
        /// Represents a collection of predefined image aspect ratios commonly used for portrait-oriented images.
        /// </summary>
        /// <remarks>The collection includes aspect ratios such as 4:5, 3:4, 2:3, and 9:16, which are typically used
        /// in applications such as photography, social media, and graphic design where portrait-oriented images are
        /// required.</remarks>
        public static readonly ImageAspectRatio[] Portraits = [Ratio4x5, Ratio3x4, Ratio2x3, Ratio9x16];

        public static implicit operator string(ImageAspectRatio obj)
            => obj._value;

        public static implicit operator ImageAspectRatio(string value)
            => value switch
            {
                "21:9" => Ratio21x9,
                "16:9" => Ratio16x9,
                "3:2"  => Ratio3x2,
                "4:3"  => Ratio4x3,
                "5:4"  => Ratio5x4,
                "1:1"  => Ratio1x1,
                "4:5"  => Ratio4x5,
                "3:4"  => Ratio3x4,
                "2:3"  => Ratio2x3,
                "9:16" => Ratio9x16,
                _ => throw new InvalidCastException($"Unknown image aspect ratio '{value}'."),
            };

        public static bool operator ==(ImageAspectRatio left, ImageAspectRatio right) 
            => left.Equals(right);

        public static bool operator !=(ImageAspectRatio left, ImageAspectRatio right) 
            => !left.Equals(right);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) 
            => obj is ImageAspectRatio other && Equals(other);

        public bool Equals(ImageAspectRatio other) 
            => _value.Equals(other._value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() 
            => _value.GetHashCode();

        public override string ToString() 
            => _value;
    }
}
