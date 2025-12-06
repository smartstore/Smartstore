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
        public static AIImageOutput Default => new() { AspectRatios = ["1:1"], Resolutions = ["1K"], Formats = ["jpeg"] };

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

        /// <summary>
        /// Gets the default aspect ratio for this model instance.
        /// </summary>
        public string? DefaultAspectRatio { get; set; }

        /// <summary>
        /// Gets the default resolution for this model instance.
        /// </summary>
        public string? DefaultResolution { get; set; }

        /// <summary>
        /// Gets the default image format for this model instance.
        /// </summary>
        public string? DefaultFormat { get; set; }

        /// <summary>
        /// Gets a value indicating whether default values should be omitted during API calls.
        /// </summary>
        public bool OmitDefault { get; set; }

        public ImageAspectRatio FindSupportedAspectRatio(
            ImageAspectRatio? attemptedRatio, 
            ImageOrientation defaultOrientation, 
            out bool isDefault)
        {
            isDefault = false;

            string[] supportedRatios = AspectRatios.IsNullOrEmpty() ? Default.AspectRatios! : AspectRatios!;

            // Check if the attempted ratio is supported
            if (attemptedRatio.HasValue)
            {
                var ratio = attemptedRatio.Value;
                if (supportedRatios.Contains(ratio.Value))
                {
                    isDefault = ratio.Value == DefaultAspectRatio || supportedRatios.Length == 1;
                    return ratio;
                }
            }

            // First check if DefaultAspectRatio meets Orientation
            if (DefaultAspectRatio != null && ((ImageAspectRatio)DefaultAspectRatio!).Orientation == defaultOrientation)
            {
                isDefault = true;
                return (ImageAspectRatio)DefaultAspectRatio!;
            }

            // Find a ratio that matches the default orientation
            foreach (var ratioStr in supportedRatios)
            {
                ImageAspectRatio? ratio = ratioStr;
                if (ratio?.Orientation == defaultOrientation)
                {
                    isDefault = ratio.Value == DefaultAspectRatio || supportedRatios.Length == 1;
                    return ratio.Value;
                }
            }

            isDefault = true;
            var defaultRatio = DefaultAspectRatio ?? supportedRatios[0];

            return (ImageAspectRatio)defaultRatio!;
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
        public AIImageOutputFormat FindSupportedFormat(AIImageOutputFormat? attemptedFormat, out bool isDefault)
        {
            string[] supportedFormats = Formats.IsNullOrEmpty() ? Default.Formats! : Formats!;

            if (attemptedFormat.HasValue)
            {
                var format = attemptedFormat.Value;
                if (supportedFormats.Contains(format.Value))
                {
                    isDefault = format.Value == DefaultFormat || supportedFormats.Length == 1;
                    return format;
                }
            }

            isDefault = true;
            var defaultFormat = DefaultFormat ?? supportedFormats[0];

            return (AIImageOutputFormat)defaultFormat!;
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
        public AIImageResolution FindSupportedResolution(AIImageResolution? attemptedResolution, out bool isDefault)
        {
            string[] supportedResolutions = Resolutions.IsNullOrEmpty() ? Default.Resolutions! : Resolutions!;

            if (attemptedResolution.HasValue)
            {
                var resolution = attemptedResolution.Value;
                if (supportedResolutions.Contains(resolution.Value))
                {
                    isDefault = resolution.Value == DefaultResolution || supportedResolutions.Length == 1;
                    return resolution;
                }
            }

            isDefault = true;
            var defaultResolution = DefaultResolution ?? supportedResolutions[0];

            return (AIImageResolution)defaultResolution!;
        }
    }
}
