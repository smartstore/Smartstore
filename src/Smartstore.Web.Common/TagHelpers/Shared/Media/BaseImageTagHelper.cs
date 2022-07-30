using Humanizer;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Imaging;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    public abstract class BaseImageTagHelper : BaseMediaTagHelper
    {
        protected const string ModelAttributeName = "sm-model";
        const string SizeAttributeName = "sm-size";
        const string WidthAttributeName = "sm-width";
        const string HeightAttributeName = "sm-height";
        const string ResizeModeAttributeName = "sm-resize-mode";
        const string AnchorPosAttributeName = "sm-anchor-position";
        const string NoFallbackAttributeName = "sm-no-fallback";

        /// <summary>
        /// The composite image model that summarizes all image attributes.
        /// </summary>
        [HtmlAttributeName(ModelAttributeName)]
        public IImageModel Model { get; set; }

        /// <summary>
        /// The max physical size (either width or height) to resize the image to.
        /// </summary>
        [HtmlAttributeName(SizeAttributeName)]
        public int? Size { get; set; }

        /// <summary>
        /// The max physical width to resize the image to.
        /// </summary>
        [HtmlAttributeName(WidthAttributeName)]
        public int? Width { get; set; }

        /// <summary>
        /// The max physical width to resize the image to.
        /// </summary>
        [HtmlAttributeName(HeightAttributeName)]
        public int? Height { get; set; }

        /// <summary>
        /// The resize mode to apply during resizing. Defaults to <see cref="ResizeMode.Max"/>.
        /// </summary>
        [HtmlAttributeName(ResizeModeAttributeName)]
        public ResizeMode? ResizeMode { get; set; }

        /// <summary>
        /// The anchor position for (crop) resizing. Defaults to <see cref="AnchorPosition.Center"/>.
        /// </summary>
        [HtmlAttributeName(AnchorPosAttributeName)]
        public AnchorPosition? AnchorPosition { get; set; }

        /// <summary>
        /// If <c>true</c>, output will be suppressed when url generation fails.
        /// </summary>
        [HtmlAttributeName(NoFallbackAttributeName)]
        public bool NoFallback { get; set; }

        [HtmlAttributeNotBound]
        internal ProcessImageQuery ImageQuery { get; set; }

        protected override Task PrepareModelAsync()
        {
            if (Model != null)
            {
                File ??= Model.File;
                Size ??= Model.ThumbSize;
                Host ??= Model.Host;
                NoFallback = Model.NoFallback;
            }

            return base.PrepareModelAsync();
        }

        protected override string GenerateMediaUrl()
        {
            ImageQuery = BuildImageQuery();
            return MediaUrlGenerator.GenerateUrl(File, ImageQuery.ToQueryString(), Host, !NoFallback);
        }

        protected virtual ProcessImageQuery BuildImageQuery()
        {
            var query = new ProcessImageQuery();

            if (Size > 0)
            {
                query.MaxSize = Size.Value;
            }

            if (Width > 0)
            {
                query.MaxWidth = Width.Value;
            }

            if (Height > 0)
            {
                query.MaxHeight = Height.Value;
            }

            if (ResizeMode.HasValue)
            {
                query.ScaleMode = ResizeMode.Value.ToString().ToLower();
            }

            if (AnchorPosition.HasValue)
            {
                query.AnchorPosition = AnchorPosition.Value.ToString().Kebaberize();
            }

            return query;
        }
    }
}
