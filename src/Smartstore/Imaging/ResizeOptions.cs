using System.Drawing;

namespace Smartstore.Imaging
{
    /// <summary>
    /// Enumerated resize modes to apply to resized images.
    /// </summary>
    public enum ResizeMode
    {
        /// <summary>
        /// Crops the resized image to fit the bounds of its container.
        /// </summary>
        Crop,

        /// <summary>
        /// Pads the resized image to fit the bounds of its container.
        /// If only one dimension is passed, will maintain the original aspect ratio.
        /// </summary>
        Pad,

        /// <summary>
        /// Pads the image to fit the bound of the container without resizing the
        /// original source.
        /// When downscaling, performs the same functionality as <see cref="Pad"/>
        /// </summary>
        BoxPad,

        /// <summary>
        /// Constrains the resized image to fit the bounds of its container maintaining
        /// the original aspect ratio. 
        /// </summary>
        Max,

        /// <summary>
        /// Resizes the image until the shortest side reaches the set given dimension.
        /// Upscaling is disabled in this mode and the original image will be returned
        /// if attempted.
        /// </summary>
        Min,

        /// <summary>
        /// Stretches the resized image to fit the bounds of its container.
        /// </summary>
        Stretch,

        /// <summary>
        /// The target location and size of the resized image has been manually set.
        /// </summary>
        Manual
    }

    /// <summary>
    /// Enumerated anchor positions to apply to resized images.
    /// </summary>
    public enum AnchorPosition
    {
        /// <summary>
        /// Anchors the position of the image to the center of it's bounding container.
        /// </summary>
        Center,

        /// <summary>
        /// Anchors the position of the image to the top of it's bounding container.
        /// </summary>
        Top,

        /// <summary>
        /// Anchors the position of the image to the bottom of it's bounding container.
        /// </summary>
        Bottom,

        /// <summary>
        /// Anchors the position of the image to the left of it's bounding container.
        /// </summary>
        Left,

        /// <summary>
        /// Anchors the position of the image to the right of it's bounding container.
        /// </summary>
        Right,

        /// <summary>
        /// Anchors the position of the image to the top left side of it's bounding container.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Anchors the position of the image to the top right side of it's bounding container.
        /// </summary>
        TopRight,

        /// <summary>
        /// Anchors the position of the image to the bottom right side of it's bounding container.
        /// </summary>
        BottomRight,

        /// <summary>
        /// Anchors the position of the image to the bottom left side of it's bounding container.
        /// </summary>
        BottomLeft
    }

    /// <summary>
    /// Enumerates known resampling algorithms
    /// </summary>
    public enum ResamplingMode
    {
        /// <summary>
        /// Bicubic sampler that implements the bicubic kernel algorithm W(x)
        /// </summary>
        Bicubic,

        /// <summary>
        /// Box sampler that implements the box algorithm. Similar to nearest neighbor when upscaling.
        /// When downscaling the pixels will average, merging pixels together.
        /// </summary>
        Box,

        /// <summary>
        /// Catmull-Rom sampler, a well known standard Cubic Filter often used as a interpolation function
        /// </summary>
        CatmullRom,

        /// <summary>
        /// Hermite sampler. A type of smoothed triangular interpolation filter that rounds off strong edges while
        /// preserving flat 'color levels' in the original image.
        /// </summary>
        Hermite,

        /// <summary>
        /// Lanczos kernel sampler that implements smooth interpolation with a radius of 2 pixels.
        /// This algorithm provides sharpened results when compared to others when downsampling.
        /// </summary>
        Lanczos2,

        /// <summary>
        /// Lanczos kernel sampler that implements smooth interpolation with a radius of 3 pixels
        /// This algorithm provides sharpened results when compared to others when downsampling.
        /// </summary>
        Lanczos3,

        /// <summary>
        /// Lanczos kernel sampler that implements smooth interpolation with a radius of 5 pixels
        /// This algorithm provides sharpened results when compared to others when downsampling.
        /// </summary>
        Lanczos5,

        /// <summary>
        /// Lanczos kernel sampler that implements smooth interpolation with a radius of 8 pixels
        /// This algorithm provides sharpened results when compared to others when downsampling.
        /// </summary>
        Lanczos8,

        /// <summary>
        ///  Mitchell-Netravali sampler. This seperable cubic algorithm yields a very good equilibrium between
        /// detail preservation (sharpness) and smoothness.
        /// </summary>
        MitchellNetravali,

        /// <summary>
        /// Nearest-Neighbour sampler that implements the nearest neighbor algorithm. This uses a very fast, unscaled filter
        /// which will select the closest pixel to the new pixels position.
        /// </summary>
        NearestNeighbor,

        /// <summary>
        /// Robidoux sampler. This algorithm developed by Nicolas Robidoux providing a very good equilibrium between
        /// detail preservation (sharpness) and smoothness comparable to <see cref="MitchellNetravali"/>.
        /// </summary>
        Robidoux,

        /// <summary>
        /// Robidoux Sharp sampler. A sharpened form of the <see cref="Robidoux"/> sampler
        /// </summary>
        RobidouxSharp,

        /// <summary>
        /// Spline sampler. A separable cubic algorithm similar to <see cref="MitchellNetravali"/> but yielding smoother results.
        /// </summary>
        Spline,

        /// <summary>
        /// Triangle sampler, otherwise known as Bilinear. This interpolation algorithm can be used where perfect image transformation
        /// with pixel matching is impossible, so that one can calculate and assign appropriate intensity values to pixels
        /// </summary>
        Triangle,

        /// <summary>
        /// Welch sampler. A high speed algorithm that delivers very sharpened results.
        /// </summary>
        Welch
    }

    public class ResizeOptions
    {
        /// <summary>
        /// Gets or sets the target size.
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Gets or sets the resize mode. Defaults to <see cref="ResizeMode.Max"/>.
        /// </summary>
        public ResizeMode Mode { get; set; } = ResizeMode.Max;

        /// <summary>
        /// Gets or sets the anchor position. Defaults to <see cref="AnchorPosition.Center"/>.
        /// </summary>
        public AnchorPosition Position { get; set; } = AnchorPosition.Center;

        /// <summary>
        /// Gets or sets the center coordinates.
        /// </summary>
        public PointF? CenterCoordinates { get; set; }

        /// <summary>
        /// Gets or sets the resampling algorithm to perform the resize operation. Defaults to <see cref="ResamplingMode.Bicubic"/>.
        /// </summary>
        public ResamplingMode Resampling { get; set; } = ResamplingMode.Bicubic;

        /// <summary>
        /// Gets or sets a value indicating whether to compress
        /// or expand individual pixel colors the value on processing.
        /// </summary>
        public bool Compand { get; set; }

        /// <summary>
        /// Gets or sets the target rectangle to resize into.
        /// </summary>
        public Rectangle? TargetRectangle { get; set; }

        /// <summary>
        /// Returns a value that indicates whether the specified object is an 
        /// <see cref="ResizeOptions"/> object that is equivalent to 
        /// this <see cref="ResizeOptions"/> object.
        /// </summary>
        /// <param name="obj">
        /// The object to test.
        /// </param>
        /// <returns>
        /// True if the given object  is an <see cref="ResizeOptions"/> object that is equivalent to 
        /// this <see cref="ResizeOptions"/> object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is not ResizeOptions other)
            {
                return false;
            }

            return this.Size == other.Size
                && this.Mode == other.Mode
                && this.Position == other.Position
                && this.CenterCoordinates == other.CenterCoordinates
                && this.Resampling == other.Resampling
                && this.Compand == other.Compand
                && this.TargetRectangle == other.TargetRectangle;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer that is the hash code for this instance.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Mode;
                hashCode = (hashCode * 397) ^ (int)Position;
                hashCode = (hashCode * 397) ^ (CenterCoordinates?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (int)Resampling;
                hashCode = (hashCode * 397) ^ Compand.GetHashCode();
                return (hashCode * 397) ^ (hashCode * 397) ^ (TargetRectangle?.GetHashCode() ?? 0);
            }
        }
    }
}
