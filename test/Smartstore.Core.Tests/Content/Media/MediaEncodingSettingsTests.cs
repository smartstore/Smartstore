using System.IO;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Imaging;
using Smartstore.Imaging.Adapters.ImageSharp;
using JpegColorType = Smartstore.Imaging.JpegColorType;
using SharpJpegColorType = SixLabors.ImageSharp.Formats.Jpeg.JpegColorType;

namespace Smartstore.Core.Tests.Content.Media;

[TestFixture]
public class MediaEncodingSettingsTests
{
    [TestCase("Octree", 0)]
    [TestCase("WebSafePalette", 1)]
    [TestCase("WernerPalette", 2)]
    [TestCase("Wu", 3)]
    public void Persisted_quantization_names_and_values_remain_compatible(string stored, int expected)
    {
        var converter = TypeConverterFactory.GetConverter(typeof(QuantizationMethod));
        var method = (QuantizationMethod)converter.ConvertFrom(stored);

        Assert.That((int)method, Is.EqualTo(expected));
        Assert.That(converter.ConvertTo(method, typeof(string)), Is.EqualTo(stored));
        Assert.That(ImageSharpUtility.CreateQuantizer(method), Is.Not.Null);
        if (method == QuantizationMethod.Octree)
        {
            Assert.That(ImageSharpUtility.CreateQuantizer(method), Is.TypeOf<HexadecatreeQuantizer>());
        }
    }

    [TestCase("YCbCrRatio420", 0, SharpJpegColorType.YCbCrRatio420)]
    [TestCase("YCbCrRatio444", 1, SharpJpegColorType.YCbCrRatio444)]
    [TestCase("YCbCrRatio422", 2, SharpJpegColorType.YCbCrRatio422)]
    [TestCase("YCbCrRatio411", 3, SharpJpegColorType.YCbCrRatio411)]
    [TestCase("YCbCrRatio410", 4, SharpJpegColorType.YCbCrRatio410)]
    [TestCase("Luminance", 5, SharpJpegColorType.Luminance)]
    [TestCase("Rgb", 6, SharpJpegColorType.Rgb)]
    public void Persisted_jpeg_color_types_produce_the_requested_encoding(string stored, int expected, SharpJpegColorType encoded)
    {
        var converter = TypeConverterFactory.GetConverter(typeof(JpegColorType));
        var colorType = (JpegColorType)converter.ConvertFrom(stored);
        Assert.That((int)colorType, Is.EqualTo(expected));
        Assert.That(converter.ConvertTo(colorType, typeof(string)), Is.EqualTo(stored));

        var format = ImageSharpUtility.CreateFormat(SharpImageFactory.FindInternalImageFormat("jpg"));
        using var image = new SharpImage(new Image<Rgba32>(32, 32, new Rgba32(50, 100, 150)), format);
        var processor = new TestImageProcessor(new MediaSettings { JpegColorType = colorType });
        processor.Process(image);

        Assert.That(((JpegEncoder)format.CreateEncoder()).ColorType, Is.EqualTo(encoded));
        using var stream = new MemoryStream();
        image.Save(stream);
        stream.Position = 0;
        using var decoded = Image.Load(stream);
        Assert.That(decoded.Metadata.GetJpegMetadata().ColorType, Is.EqualTo(encoded));
    }

    [TestCase(true, 1)]
    [TestCase(false, 0)]
    public void Png_interlacing_matches_the_saved_setting(bool interlaced, int expected)
    {
        using var image = new SharpImage(new Image<Rgba32>(16, 16, new Rgba32(50, 100, 150)));
        var processor = new TestImageProcessor(new MediaSettings { PngInterlaced = interlaced });
        processor.Process(image);

        using var stream = new MemoryStream();
        image.Save(stream);

        // The last byte of the PNG IHDR chunk data contains the interlace method.
        Assert.That(stream.ToArray()[28], Is.EqualTo(expected));
    }

    [Test]
    public void Png_palette_setting_does_not_reduce_truecolor_images()
    {
        using var image = new SharpImage(new Image<Rgba32>(16, 16, new Rgba32(50, 100, 150, 123)));
        var processor = new TestImageProcessor(new MediaSettings { PngQuantizationMethod = QuantizationMethod.WebSafePalette });
        processor.Process(image);

        using var stream = new MemoryStream();
        image.Save(stream);
        stream.Position = 0;
        using var decoded = Image.Load<Rgba32>(stream);
        Assert.That(decoded[0, 0], Is.EqualTo(new Rgba32(50, 100, 150, 123)));
    }

    private sealed class TestImageProcessor(MediaSettings settings) : DefaultImageProcessor(null, null, settings)
    {
        public void Process(IProcessableImage image)
            => ProcessImageCore(new ProcessImageQuery(), image, out _);
    }
}
