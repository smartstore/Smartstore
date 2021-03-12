using System.Drawing;

namespace Smartstore.Imaging
{
    public interface IImageTransformer
    {
        /// <summary>
        /// Gets the current processed image.
        /// </summary>
        IProcessableImage Image { get; }

        /// <summary>
        /// Gets the image dimensions at the current point in the processing pipeline.
        /// </summary>
        public Size CurrentSize { get; }


        /// <summary>
        /// Adjusts an image so that its orientation is suitable for viewing. Adjustments are based on EXIF metadata embedded in the image.
        /// </summary>
        IImageTransformer AutoOrient();

        /// <summary>
        /// Changes the background color of the current image.
        /// </summary>
        /// <param name="color">
        /// The <see cref="T:System.Drawing.Color"/> to paint the image background with.
        /// </param>
        IImageTransformer BackgroundColor(Color color);

        /// <summary>
        /// Applies black and white toning to the image.
        /// </summary>
        IImageTransformer BlackWhite();

        /// <summary>
        /// Applies a bokeh blur to the image.
        /// </summary>
        /// <param name="radius">The 'radius' value representing the size of the area to sample.</param>
        /// <param name="components">The 'components' value representing the number of kernels to use to approximate the bokeh effect. Range 1..6.</param>
        /// <param name="gamma">The gamma highlight factor to use to emphasize bright spots in the source image. Must be &gt;= 1.</param>
        IImageTransformer BokehBlur(int radius = 32, int components = 2, float gamma = 3F);

        /// <summary>
        /// Applies a box blur to the image.
        /// </summary>
        /// <param name="radius">The 'radius' value representing the size of the area to sample.</param>
        IImageTransformer BoxBlur(int radius = 7);

        /// <summary>
        /// Alters the brightness component of the image.
        /// </summary>
        /// <remarks>
        /// A value of 0 will create an image that is completely black. A value of 1 leaves the input unchanged.
        /// Other values are linear multipliers on the effect. Values of an amount over 1 are allowed, providing brighter results.
        /// </remarks>
        /// <param name="amount">The proportion of the conversion. Must be greater than or equal to 0.</param>
        IImageTransformer Brightness(float amount);


        /// <summary>
        /// Alters the contrast component of the image.
        /// </summary>
        /// <remarks>
        /// A value of 0 will create an image that is completely gray. A value of 1 leaves the input unchanged.
        /// Other values are linear multipliers on the effect. Values of an amount over 1 are allowed, providing results with more contrast.
        /// </remarks>
        /// <param name="amount">The proportion of the conversion. Must be greater than or equal to 0.</param>
        IImageTransformer Contrast(float amount);

        /// <summary>
        /// Crops an image to the given rectangle.
        /// </summary>
        /// <param name="cropRectangle">The <see cref="Rectangle"/> structure that specifies the portion of the image object to retain.</param>
        IImageTransformer Crop(Rectangle rect);

        /// <summary>
        /// Dithers the image reducing it to a web-safe palette using the "Floyd Steinberg" algorithm.
        /// </summary>
        /// <param name="mode">The dithering mode.</param>
        /// <param name="ditherScale">The dithering scale used to adjust the amount of dither. Range 0..1.</param>
        IImageTransformer Dither(DitheringMode mode = DitheringMode.FloydSteinberg, float ditherScale = 1F);

        /// <summary>
        /// Draws the given image together with the current one by blending their pixels.
        /// </summary>
        /// <param name="image">The image to blend with the currently processing image.</param>
        /// <param name="position">The position to draw the blended image.</param>
        /// <param name="colorBlending">The color blending to apply.</param>
        /// <param name="alphaComposition">The alpha composition mode.</param>
        /// <param name="opacity">The opacity of the image to blend. Must be between 0 and 1.</param>
        IImageTransformer DrawImage(IImage image, Point position, PixelColorBlendingMode colorBlending, PixelAlphaCompositionMode alphaComposition, float opacity);

        /// <summary>
        /// Crops an image to the area of greatest entropy.
        /// </summary>
        /// <param name="threshold">The threshold for entropic density. Must be between 0 and 1.</param>
        IImageTransformer EntropyCrop(float threshold = .5f);

        /// <summary>
        /// Flips an image by the given instructions.
        /// </summary>
        /// <param name="mode">The <see cref="FlipMode"/> to perform the flip.</param>
        IImageTransformer Flip(FlipMode mode);

        /// <summary>
        /// Applies a Gaussian blur to the image.
        /// </summary>
        /// <param name="sigma">The 'sigma' value representing the weight of the blur.</param>
        IImageTransformer GaussianBlur(float sigma = 3f);

        /// <summary>
        /// Applies a Gaussian sharpening filter to the image.
        /// </summary>
        /// <param name="sigma">The 'sigma' value representing the weight of the blur.</param>
        IImageTransformer GaussianSharpen(float sigma = 3f);

        /// <summary>
        /// Applies grayscale toning to the image using the given amount.
        /// </summary>
        /// <param name="amount">The proportion of the conversion. Range 0..1.</param>
        IImageTransformer Grayscale(float amount = 1F);

        /// <summary>
        /// Alters the hue component of the image.
        /// </summary>
        /// <param name="degrees">The rotation angle in degrees to adjust the hue.</param>
        IImageTransformer Hue(float degrees);

        /// <summary>
        /// Inverts the colors of the image.
        /// </summary>
        IImageTransformer Invert();

        /// <summary>
        /// Alters the colors of the image recreating an old Kodachrome camera effect.
        /// </summary>
        IImageTransformer Kodachrome();

        /// <summary>
        /// Alters the lightness component of the image.
        /// </summary>
        /// <remarks>
        /// A value of 0 will create an image that is completely black. A value of 1 leaves the input unchanged.
        /// Other values are linear multipliers on the effect. Values of an amount over 1 are allowed, providing lighter results.
        /// </remarks>
        /// <param name="amount">The proportion of the conversion. Must be greater than or equal to 0.</param>
        IImageTransformer Lightness(float amount);

        /// <summary>
        /// Alters the colors of the image recreating an oil painting effect.
        /// </summary>
        /// <param name="levels">The number of intensity levels. Higher values result in a broader range of color intensities forming part of the result image.</param>
        /// <param name="brushSize">The number of neighboring pixels used in calculating each individual pixel value.</param>
        IImageTransformer OilPaint(int levels = 10, int brushSize = 15);

        /// <summary>
        /// Multiplies the alpha component of the image.
        /// </summary>
        /// <param name="amount">The proportion of the conversion. Must be between 0 and 1.</param>
        IImageTransformer Opacity(float amount);

        /// <summary>
        /// Resizes the current image according to the given options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>
        IImageTransformer Resize(ResizeOptions options);

        /// <summary>
        /// Rotates an image by the given angle in degrees.
        /// </summary>
        /// <param name="degrees">The angle in degrees to perform the rotation.</param>
        IImageTransformer Rotate(float degrees);

        /// <summary>
        /// Alters the saturation component of the image.
        /// </summary>
        /// <remarks>
        /// A value of 0 is completely un-saturated. A value of 1 leaves the input unchanged.
        /// Other values are linear multipliers on the effect. Values of amount over 1 are allowed, providing super-saturated results
        /// </remarks>
        /// <param name="amount">The proportion of the conversion. Must be greater than or equal to 0.</param>
        IImageTransformer Saturate(float amount);

        /// <summary>
        /// Applies sepia toning to the image using the given amount.
        /// </summary>
        /// <param name="amount">The proportion of the conversion. Range 0..1.</param>
        IImageTransformer Sepia(float amount = 1F);

        /// <summary>
        /// Applies a radial vignette effect to an image.
        /// </summary>
        /// <param name="color">The color to set as the vignette.</param>
        IImageTransformer Vignette(Color? color = null);

        /// <summary>
        /// Applies a radial vignette effect to an image.
        /// </summary>
        /// <param name="color">The color to set as the vignette.</param>
        /// <param name="radiusX">The the x-radius.</param>
        /// <param name="radiusY">The the y-radius.</param>
        /// <param name="rectangle">
        /// The <see cref="Rectangle"/> structure that specifies the portion of the image object to alter.
        /// </param>
        IImageTransformer Vignette(Color color, float radiusX, float radiusY, Rectangle rect);
    }

    public static class IImageTransformerExtensions
    {
        /// <summary>
        /// Resizes the current image to the given dimensions.
        /// </summary>
        /// <param name="size">
        /// The <see cref="T:System.Drawing.Size"/> containing the width and height to set the image to.
        /// </param>
        /// <returns>
        public static IImageTransformer Resize(this IImageTransformer transformer, Size size)
        {
            return transformer.Resize(new ResizeOptions { Size = size });
        }

        /// <summary>
        /// Resizes the current image to the given max size while preserving aspect ratio.
        /// </summary>
        /// <param name="maxSize">
        /// The maximum size to resize an image to. Used to restrict resizing based on calculated resizing.
        /// </param>
        /// <returns>
        public static IImageTransformer Resize(this IImageTransformer transformer, int maxSize)
        {
            var size = ImagingHelper.Rescale(transformer.CurrentSize, maxSize);
            return transformer.Resize(new ResizeOptions { Size = size });
        }

        /// <summary>
        /// Resizes the current image to the given max size while preserving aspect ratio.
        /// </summary>
        /// <param name="maxSize">
        /// The maximum size to resize an image to. Used to restrict resizing based on calculated resizing.
        /// </param>
        /// <param name="options">The options.</param>
        /// <returns>
        public static IImageTransformer Resize(this IImageTransformer transformer, int maxSize, ResizeOptions options)
        {
            Guard.NotNull(options, nameof(options));

            options.Size = ImagingHelper.Rescale(transformer.CurrentSize, maxSize);
            return transformer.Resize(options);
        }

        /// <summary>
        /// Crops an image to the given width and height.
        /// </summary>
        /// <param name="width">The target image width.</param>
        /// <param name="height">The target image height.</param>
        public static IImageTransformer Crop(this IImageTransformer transformer, int width, int height)
        {
            return transformer.Crop(new Rectangle(0, 0, width, height));
        }

        /// <summary>
        /// Evenly pads an image to fit the new dimensions with the given background color.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        /// <param name="color">The background color with which to pad the image.</param>
        public static IImageTransformer Pad(this IImageTransformer transformer, int width, int height, Color color = default)
        {
            var options = new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.BoxPad,
                Resampling = ResamplingMode.NearestNeighbor
            };

            transformer.Resize(options);

            if (!color.Equals(default))
            {
                transformer.BackgroundColor(color);
            }

            return transformer;
        }
    }
}
