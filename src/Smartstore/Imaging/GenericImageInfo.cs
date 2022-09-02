namespace Smartstore.Imaging
{
    public sealed class GenericImageInfo : IImageInfo
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public byte BitDepth { get; set; } = 24;

        public IImageFormat Format { get; set; }

        public IEnumerable<ImageMetadataEntry> GetMetadata()
            => Enumerable.Empty<ImageMetadataEntry>();
    }
}
