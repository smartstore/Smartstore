using System.Drawing;
using System.Text;
using System.Xml;
using Autofac;
using Smartstore.Engine;
using Smartstore.IO;

namespace Smartstore.Imaging
{
    /// <summary>
    /// Taken from http://stackoverflow.com/questions/111345/getting-image-dimensions-without-reading-the-entire-file/111349
    /// Minor improvements including supporting unsigned 16-bit integers when decoding Jfif and added logic
    /// to load the image using new Bitmap if reading the headers fails.
    /// </summary>
    public static class ImageHeader
    {
        // Maybe removing this class in favor of ImageFactory.Detect* (in release mode) is the better choice (?)

        internal class UnknownImageFormatException : ArgumentException
        {
            public UnknownImageFormatException(string paramName = "", Exception e = null)
                : base("Could not recognise image format.", paramName, e)
            {
            }
        }

        private static readonly List<(string Format, byte[] Marker, Func<BinaryReader, Size> Parser)> _imageFormatDecoders = new()
        {
            ("jpg", new byte[] { 0xff, 0xd8 }, DecodeJpeg),
            ("png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng),
            ("gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif),
            ("gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif),
            ("bmp", new byte[] { 0x42, 0x4D }, DecodeBitmap),
        };

        private static readonly int _maxMagicBytesLength = 0;
        private static IImageFactory _imageFactory;

        static ImageHeader()
        {
            _maxMagicBytesLength = _imageFormatDecoders.OrderByDescending(x => x.Marker.Length).First().Marker.Length;
        }

        internal static IImageFactory ImageFactory
        {
            get => _imageFactory ??= EngineContext.Current?.Application?.Services?.ResolveOptional<IImageFactory>();
            // For unit tests
            set => _imageFactory = value;
        }

        /// <summary>        
        /// Gets the dimensions of an image.        
        /// </summary>        
        /// <param name="path">The path of the image to get dimensions for.</param>        
        /// <returns>The dimensions of the specified image.</returns>       
        public static Size GetPixelSize(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File '{0}' does not exist.".FormatInvariant(path));
            }

            var mime = MimeTypes.MapNameToMimeType(path);
            return GetPixelSizeWithFormat(File.OpenRead(path), mime, false).Size;
        }

        /// <summary>        
        /// Gets the dimensions of an image.        
        /// </summary>        
        /// <param name="buffer">The bytes of the image to get the dimensions for.</param>
        /// <param name="mime">The MIME type of the image. Can be <c>null</c>.</param> 
        /// <returns>The dimensions of the specified image.</returns>    
        public static Size GetPixelSize(byte[] buffer, string mime = null)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return Size.Empty;
            }

            return GetPixelSizeWithFormat(new MemoryStream(buffer), mime, false).Size;
        }

        /// <summary>        
        /// Gets the dimensions of an image.        
        /// </summary>        
        /// <param name="input">The stream of the image to get the dimensions for.</param>    
        /// <param name="leaveOpen">If false, the passed stream will get disposed</param>
        /// <returns>The dimensions of the specified image.</returns>    
        public static Size GetPixelSize(Stream input, bool leaveOpen = true)
        {
            return GetPixelSizeWithFormat(input, null, leaveOpen).Size;
        }

        /// <summary>        
        /// Gets the dimensions and the format of an image.        
        /// </summary>        
        /// <param name="input">The stream of the image to get the dimensions for.</param>    
        /// <param name="leaveOpen">If false, the passed stream will get disposed</param>
        /// <returns>The dimensions of the specified image.</returns>    
        public static (Size Size, IImageFormat Format) GetPixelSizeWithFormat(Stream input, bool leaveOpen = true)
        {
            return GetPixelSizeWithFormat(input, null, leaveOpen);
        }

        /// <summary>        
        /// Gets the dimensions of an image.        
        /// </summary>        
        /// <param name="input">The stream of the image to get the dimensions for.</param> 
        /// <param name="mime">The MIME type of the image. Can be <c>null</c>.</param> 
        /// <param name="leaveOpen">If false, the passed stream will get disposed</param>
        /// <returns>The dimensions of the specified image.</returns>    
        public static Size GetPixelSize(Stream input, string mime, bool leaveOpen = true)
        {
            return GetPixelSizeWithFormat(input, mime, leaveOpen).Size;
        }

        /// <summary>        
        /// Gets the dimensions and the format of an image.        
        /// </summary>        
        /// <param name="input">The stream of the image to get the dimensions for.</param> 
        /// <param name="mime">The MIME type of the image. Can be <c>null</c>.</param> 
        /// <param name="leaveOpen">If false, the passed stream will get disposed</param>
        /// <returns>The dimensions of the specified image.</returns>    
        public static (Size Size, IImageFormat Format) GetPixelSizeWithFormat(Stream input, string mime, bool leaveOpen = true)
        {
            Guard.NotNull(input);

            var slowDetect = false;

            if (leaveOpen && (!input.CanSeek || input.Length == 0))
            {
                return (Size.Empty, null);
            }

            try
            {
                if (mime == "image/svg+xml")
                {
                    return (GetPixelSizeFromSvg(input), null); // No format for SVG
                }

                using (var reader = new BinaryReader(input, Encoding.Unicode, true))
                {
                    var size = GetPixelSize(reader, out var format);
                    if (format.HasValue() && ImageFactory != null)
                    {
                        return (size, ImageFactory.FindFormatByExtension(format));
                    }
                    else
                    {
                        return (size, null);
                    }
                }
            }
            catch
            {
                if (slowDetect)
                {
                    throw;
                }

                // Something went wrong with fast image header access,
                // so get original size the classic way
                try
                {
                    if (input.CanSeek)
                    {
                        input.Seek(0, SeekOrigin.Begin);
                        var size = GetPixelSizeByImageFactory(input, out var imageFormat);
                        return (size, imageFormat);
                    }
                    else
                    {
                        return (Size.Empty, null);
                    }
                }
                catch
                {
                    throw;
                }
            }
            finally
            {
                if (!leaveOpen)
                {
                    input.Dispose();
                }
                else
                {
                    if (input.CanSeek)
                    {
                        input.Seek(0, SeekOrigin.Begin);
                    }
                }
            }
        }

        /// <summary>        
        /// Gets the dimensions of an image.        
        /// </summary>        
        /// <param name="path">The path of the image to get the dimensions of.</param>        
        /// <returns>The dimensions of the specified image.</returns>        
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>            
        internal static Size GetPixelSize(BinaryReader binaryReader, out string format)
        {
            format = null;

            byte[] magicBytes = new byte[_maxMagicBytesLength];

            for (int i = 0; i < _maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();
                for (int y = 0; y < _imageFormatDecoders.Count; y++)
                {
                    var decoder = _imageFormatDecoders[y];

                    if (StartsWith(magicBytes, decoder.Marker))
                    {
                        format = decoder.Format;
                        var size = decoder.Parser(binaryReader);
                        if (size.IsEmpty)
                        {
                            break;
                        }
                        else
                        {
                            return size;
                        }
                    }
                }
            }

            throw new UnknownImageFormatException(nameof(binaryReader));
        }

        private static Size GetPixelSizeByImageFactory(Stream input, out IImageFormat imageFormat)
        {
            var info = ImageFactory?.DetectInfo(input);
            if (info != null)
            {
                imageFormat = info.Format;
                return new Size(info.Width, info.Height);
            }

            imageFormat = null;
            return Size.Empty;
        }

        private static Size GetPixelSizeFromSvg(Stream input)
        {
            using (var reader = XmlReader.Create(input))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        if (reader.Name == "svg")
                        {
                            var width = reader["width"];
                            var height = reader["height"];

                            var size = new Size(width.ToInt(), height.ToInt());
                            if (size.Width == 0 || size.Height == 0)
                            {
                                var viewBox = reader["viewBox"];
                                if (viewBox.HasValue())
                                {
                                    var arrViewBox = viewBox.Trim().Split(' ');
                                    if (arrViewBox.Length == 4)
                                    {
                                        size = new Size(arrViewBox[2].ToInt(), arrViewBox[3].ToInt());
                                    }
                                }
                            }

                            return size;
                        }
                    }
                }
            }

            return Size.Empty;
        }

        private static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
        {
            for (int i = 0; i < thatBytes.Length; i += 1)
            {
                if (thisBytes[i] != thatBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static short ReadLittleEndianInt16(BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(short)];

            for (int i = 0; i < sizeof(short); i += 1)
            {
                bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt16(bytes, 0);
        }

        private static ushort ReadLittleEndianUInt16(BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(ushort)];

            for (int i = 0; i < sizeof(ushort); i += 1)
            {
                bytes[sizeof(ushort) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToUInt16(bytes, 0);
        }

        private static int ReadLittleEndianInt32(BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1)
            {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Size DecodeBitmap(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            int width = binaryReader.ReadInt32();
            int height = binaryReader.ReadInt32();
            return new Size(width, height);
        }

        private static Size DecodeGif(BinaryReader binaryReader)
        {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            int width = ReadLittleEndianInt32(binaryReader);
            int height = ReadLittleEndianInt32(binaryReader);
            return new Size(width, height);
        }

        private static Size DecodeJpeg(BinaryReader reader)
        {
            string state = "started";
            while (true)
            {
                byte[] c;
                if (state == "started")
                {
                    c = reader.ReadBytes(1);
                    state = (c[0] == 0xFF) ? "sof" : "started";
                }
                else if (state == "sof")
                {
                    c = reader.ReadBytes(1);
                    if (c[0] >= 0xe0 && c[0] <= 0xef)
                    {
                        state = "skipframe";
                    }
                    else if ((c[0] >= 0xC0 && c[0] <= 0xC3) || (c[0] >= 0xC5 && c[0] <= 0xC7) || (c[0] >= 0xC9 && c[0] <= 0xCB) || (c[0] >= 0xCD && c[0] <= 0xCF))
                    {
                        state = "readsize";
                    }
                    else if (c[0] == 0xFF)
                    {
                        state = "sof";
                    }
                    else
                    {
                        state = "skipframe";
                    }
                }
                else if (state == "skipframe")
                {
                    c = reader.ReadBytes(2);
                    int skip = ReadInt(c) - 2;
                    reader.ReadBytes(skip);
                    state = "started";
                }
                else if (state == "readsize")
                {
                    c = reader.ReadBytes(7);
                    var width = ReadInt(new[] { c[5], c[6] });
                    var height = ReadInt(new[] { c[3], c[4] });
                    return new Size(width, height);
                }
            }

            throw new UnknownImageFormatException();
        }

        private static int ReadInt(byte[] chars)
        {
            return (chars[0] << 8) + chars[1];
        }
    }
}
