using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Localization;
using Smartstore.Imaging;

namespace Smartstore.Web.UI.TagHelpers
{
    [HtmlTargetElement(ImageTagName, Attributes = FileAttributeName)]
    [HtmlTargetElement(ImageTagName, Attributes = FileIdAttributeName)]
    public class ImageTagHelper : SmartTagHelper
    {
        const string ImageTagName = "img";
        const string FileAttributeName = "file";
        const string FileIdAttributeName = "file-id";
        const string SizeAttributeName = "img-size";
        const string WidthAttributeName = "img-width";
        const string HeightAttributeName = "img-height";
        const string ResizeModeAttributeName = "img-resize-mode";
        const string AnchorPosAttributeName = "img-anchor-position";
        const string NoFallbackAttributeName = "img-no-fallback";
        const string HostAttributeName = "url-host";

        private readonly IMediaUrlGenerator _urlGenerator;
        private readonly IMediaService _mediaService;

        public ImageTagHelper(IMediaUrlGenerator urlGenerator, IMediaService mediaService)
        {
            _urlGenerator = urlGenerator;
            _mediaService = mediaService;
        }

        /// <summary>
        /// The <see cref="MediaFileInfo"/> instance to render an img tag for.
        /// </summary>
        public MediaFileInfo File { get; set; }

        /// <summary>
        /// The <see cref="MediaFileInfo"/> instance to render an img tag for.
        /// </summary>
        public int? FileId { get; set; }

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
        /// Store host for an absolute URL that also contains scheme and host parts. 
        /// <c>Omitting</c> this attribute tries to resolve host automatically based on <see cref="Store.ContentDeliveryNetwork"/> or <see cref="MediaSettings.AutoGenerateAbsoluteUrls"/>.
        /// <c>Empty</c> attribute value bypasses automatic host resolution and does NOT prepend host to path.
        /// <c>Any string</c> value: host name to use explicitly.
        /// </summary>
        [HtmlAttributeName(HostAttributeName)]
        public string Host { get; set; }

        /// <summary>
        /// If <c>true</c>, output will be suppressed when url generation fails.
        /// </summary>
        [HtmlAttributeName(NoFallbackAttributeName)]
        public bool NoFallback { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            await ResolveFileAsync();
            
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

            var src = _urlGenerator.GenerateUrl(File, query.ToQueryString(), Host, !NoFallback);

            if (src.IsEmpty())
            {
                output.SuppressOutput();
                return;
            }
            else
            {
                output.Attributes.SetAttribute("src", src);

                if (File.Alt.HasValue())
                {
                    output.Attributes.SetAttributeNoReplace("alt", () => File.File.GetLocalized(x => x.Alt));
                }

                if (File.TitleAttribute.HasValue())
                {
                    output.Attributes.SetAttributeNoReplace("title", () => File.File.GetLocalized(x => x.Title));
                }
            }
        }

        protected async Task ResolveFileAsync()
        {
            if (File == null)
            {
                File = await _mediaService.GetFileByIdAsync(FileId ?? 0, MediaLoadFlags.AsNoTracking);
            }
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
