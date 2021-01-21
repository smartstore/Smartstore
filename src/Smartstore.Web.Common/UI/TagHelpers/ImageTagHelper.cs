using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Imaging;

namespace Smartstore.Web.UI.TagHelpers
{
    [HtmlTargetElement("img", Attributes = "file")]
    public class ImageTagHelper : SmartTagHelper
    {
        private readonly IMediaUrlGenerator _urlGenerator;

        public ImageTagHelper(IMediaUrlGenerator urlGenerator)
        {
            _urlGenerator = urlGenerator;
        }

        /// <summary>
        /// The <see cref="MediaFileInfo"/> instance to render an img tag for.
        /// </summary>
        public MediaFileInfo File { get; set; }

        /// <summary>
        /// The max physical size (either width or height) to resize the image to.
        /// </summary>
        [HtmlAttributeName("img-size")]
        public int? ImageSize { get; set; }

        /// <summary>
        /// The max physical width to resize the image to.
        /// </summary>
        [HtmlAttributeName("img-width")]
        public int? ImageWidth { get; set; }

        /// <summary>
        /// The max physical width to resize the image to.
        /// </summary>
        [HtmlAttributeName("img-height")]
        public int? ImageHeight { get; set; }

        /// <summary>
        /// The resize mode to apply during resizing. Defaults to <see cref="ResizeMode.Max"/>.
        /// </summary>
        [HtmlAttributeName("img-resize-mode")]
        public ResizeMode? ImageResizeMode { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            var query = new ProcessImageQuery();

            if (ImageSize > 0)
            {
                query.MaxSize = ImageSize.Value;
            }

            if (ImageWidth > 0)
            {
                query.MaxWidth = ImageWidth.Value;
            }

            if (ImageHeight > 0)
            {
                query.MaxHeight = ImageHeight.Value;
            }

            if (ImageResizeMode.HasValue)
            {
                query.ScaleMode = ImageResizeMode.Value.ToString().ToLower();
            }

            var src = _urlGenerator.GenerateUrl(File, query.ToQueryString());

            output.Attributes.SetAttribute("src", src);

            if (File.Alt.HasValue())
            {
                output.Attributes.SetAttributeNoReplace("alt", File.Alt);
            }

            if (File.TitleAttribute.HasValue())
            {
                output.Attributes.SetAttributeNoReplace("title", File.TitleAttribute);
            }
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
