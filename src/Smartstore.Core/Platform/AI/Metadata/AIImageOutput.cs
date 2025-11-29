#nullable enable

using Smartstore.Imaging;

namespace Smartstore.Core.AI.Metadata
{
    /// <summary>
    /// Represents the configuration for AI image output, including supported aspect ratios, resolutions, and formats.
    /// </summary>
    /// <remarks>This record is used to define the parameters for generating AI image outputs. It provides
    /// properties to specify supported aspect ratios, resolutions, and formats, as well as methods to determine the
    /// best match for a given attempted configuration. A default configuration is available via the <see
    /// cref="Default"/> property.</remarks>
    public record AIImageOutput
    {
        /// <summary>
        /// Gets the default configuration for AI image output.
        /// </summary>
        public static AIImageOutput Default => new() { AspectRatios = ["1:1"], Resolutions = ["1K"], Formats = ["png"] };

        /// <summary>
        /// Gets an array of supported aspect ratios. Default: 1:1.
        /// </summary>
        public string[]? AspectRatios { get; set; }

        /// <summary>
        /// Gets an array of supported resolutions. Default: 1K.
        /// </summary>
        public string[]? Resolutions { get; set; }

        /// <summary>
        /// Gets an array of supported image formats. Default: png.
        /// </summary>
        public string[]? Formats { get; set; }

        public ImageAspectRatio FindSupportedAspectRatio(ImageAspectRatio? attemptedRatio, ImageOrientation defaultOrientation)
        {
            var supportedRatios = AspectRatios.IsNullOrEmpty() ? Default.AspectRatios! : AspectRatios!;

            // Check if the attempted ratio is supported
            if (attemptedRatio.HasValue)
            {
                var ratio = attemptedRatio.Value;
                if (supportedRatios.Contains(ratio.Value))
                {
                    return ratio;
                }
            }

            // Find a ratio that matches the default orientation
            foreach (var ratioStr in supportedRatios)
            {
                ImageAspectRatio ratio = ratioStr;
                if (ratio.Orientation == defaultOrientation)
                {
                    return ratio;
                }
            }

            return supportedRatios[0];
        }

        /// <summary>
        /// Determines the supported image output format based on the specified attempted format.
        /// </summary>
        /// <remarks>If the <see cref="Formats"/> property is null or empty, the method uses the default
        /// formats defined in <c>Default.Formats</c>.</remarks>
        /// <param name="attemptedFormat">The format to attempt to use. If the format is supported, it will be returned; otherwise, the first
        /// supported format is returned.</param>
        /// <returns>The supported image output format. If <paramref name="attemptedFormat"/> is not specified or is not
        /// supported, the first format in the supported formats list is returned.</returns>
        public AIImageOutputFormat FindSupportedFormat(AIImageOutputFormat? attemptedFormat)
        {
            var supportedFormats = Formats.IsNullOrEmpty() ? Default.Formats! : Formats!;

            if (attemptedFormat.HasValue)
            {
                var format = attemptedFormat.Value;
                if (supportedFormats.Contains(format.Value))
                {
                    return format;
                }
            }

            return supportedFormats[0];
        }

        /// <summary>
        /// Determines the supported image resolution based on the attempted resolution, or returns the default
        /// resolution if the attempted resolution is not supported.
        /// </summary>
        /// <remarks>If the <paramref name="attemptedResolution"/> is not <see langword="null"/> and is 
        /// included in the list of supported resolutions, it will be returned. Otherwise, the  method returns the last
        /// resolution in the list of supported resolutions.</remarks>
        /// <param name="attemptedResolution">The resolution to attempt, or <see langword="null"/> to use the default resolution.</param>
        /// <returns>The supported resolution that matches the attempted resolution, if found;  otherwise, the default
        /// resolution.</returns>
        public AIImageResolution FindSupportedResolution(AIImageResolution? attemptedResolution)
        {
            var supportedResolutions = Resolutions.IsNullOrEmpty() ? Default.Resolutions! : Resolutions!;

            if (attemptedResolution.HasValue)
            {
                var resolution = attemptedResolution.Value;
                if (supportedResolutions.Contains(resolution.Value))
                {
                    return resolution;
                }
            }

            return supportedResolutions[0];
        }
    }
}
