using System.Numerics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Smartstore.Imaging.Barcodes.Impl
{
    internal static class EanTextRenderer
    {
        private const int UnscaledFontSize = 9;
        private const int ContentMargin = 9;
        private const int ContentVerticalOffset = 0;

        public static void Render(Image<L8> image, Barcoder.IBarcode barcode, BarcodeImageOptions options)
        {
            //var font = SystemFonts.CreateFont(options.EanFontFamily, UnscaledFontSize * options.PixelSize, FontStyle.Regular);

            //switch (barcode.Metadata.CodeKind)
            //{
            //    case Barcoder.BarcodeType.EAN8:
            //        RenderContentForEan8(image, barcode.Content, font, options);
            //        break;
            //    case Barcoder.BarcodeType.EAN13:
            //        RenderContentForEan13(image, barcode.Content, font, options);
            //        break;
            //}
        }

        //private static void RenderContentForEan8(Image<L8> image, string content, Font font, BarcodeImageOptions options)
        //{
        //    int ApplyScale(int value) => value * scale;
        //    var margin = options.Margin ?? barcode.Margin;

        //    RenderWhiteRect(image, ApplyScale(margin + 3), image.Height - ApplyScale(margin + ContentMargin), ApplyScale(29), ApplyScale(ContentMargin));
        //    RenderWhiteRect(image, ApplyScale(margin + 35), image.Height - ApplyScale(margin + ContentMargin), ApplyScale(29), ApplyScale(ContentMargin));

        //    float textTop = image.Height - (margin + ContentMargin / 2.0f - ContentVerticalOffset) * scale;
        //    float textCenter1 = (29.0f / 2.0f + margin + 3.0f) * scale;
        //    float textCenter2 = (29.0f / 2.0f + margin + 35.0f) * scale;
        //    RenderBlackText(image, content.Substring(0, 4), textCenter1, textTop, font);
        //    RenderBlackText(image, content.Substring(4), textCenter2, textTop, font);
        //}

        //private static void RenderContentForEan13(Image<L8> image, string content, Font font, BarcodeImageOptions options)
        //{
        //    int ApplyScale(int value) => value * scale;
        //    RenderWhiteRect(image, ApplyScale(margin + 3), image.Height - ApplyScale(margin + ContentMargin), ApplyScale(43), ApplyScale(ContentMargin));
        //    RenderWhiteRect(image, ApplyScale(margin + 49), image.Height - ApplyScale(margin + ContentMargin), ApplyScale(43), ApplyScale(ContentMargin));

        //    float textTop = image.Height - (margin + ContentMargin / 2.0f - ContentVerticalOffset) * scale;
        //    float textCenter1 = (margin - 4.0f) * scale;
        //    float textCenter2 = (43.0f / 2.0f + margin + 3.0f) * scale;
        //    float textCenter3 = (43.0f / 2.0f + margin + 49.0f) * scale;
        //    RenderBlackText(image, content.Substring(0, 1), textCenter1, textTop, font);
        //    RenderBlackText(image, content.Substring(1, 6), textCenter2, textTop, font);
        //    RenderBlackText(image, content.Substring(7), textCenter3, textTop, font);
        //}

        //private static void RenderWhiteRect(Image<L8> image, int x, int y, int width, int height)
        //{
        //    image.Mutate(ctx => ctx.FillPolygon(
        //        Color.White,
        //        new Vector2(x, y),
        //        new Vector2(x + width, y),
        //        new Vector2(x + width, y + height),
        //        new Vector2(x, y + height)));
        //}

        //private static void RenderBlackText(Image<L8> image, string text, float x, float y, Font font)
        //{
        //    var options = new TextOptions(font)
        //    {
        //        HorizontalAlignment = HorizontalAlignment.Center,
        //        VerticalAlignment = VerticalAlignment.Center,
        //        Origin = new PointF(x, y),
        //    };

        //    image.Mutate(ctx => ctx.DrawText(options, text, Color.Black));
        //}
    }
}
