using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Outputs suitable tag for given media type (img, audio, video, thumbnail etc.)
    /// </summary>
    [OutputElementHint("figure")]
    [HtmlTargetElement(MediaTagName, Attributes = FileAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement(MediaTagName, Attributes = FileIdAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement(MediaTagName, Attributes = ModelAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class MediaTagHelper : BaseImageTagHelper
    {
        const string MediaTagName = "media";

        private readonly ITagHelperFactory _tagHelperFactory;

        public MediaTagHelper(ITagHelperFactory tagHelperFactory)
        {
            _tagHelperFactory = tagHelperFactory;
        }

        protected override void ProcessMedia(TagHelperContext context, TagHelperOutput output)
        {
            if (Src.IsEmpty() || File == null)
            {
                output.SuppressOutput();
                return;
            }

            var tagHelper = CreateInnerTagHelper(context, output);
            if (tagHelper != null)
            {
                tagHelper.Process(context, output);
            }
            else
            {
                output.SuppressOutput();
            }
        }

        private BaseMediaTagHelper CreateInnerTagHelper(TagHelperContext context, TagHelperOutput output)
        {
            BaseMediaTagHelper tagHelper = null;

            if (File.File.MediaType == MediaType.Image)
            {
                output.TagName = "img";
                tagHelper = _tagHelperFactory.CreateTagHelper<ImageTagHelper>(ViewContext);
            }
            else if (ImageQuery?.NeedsProcessing() == true)
            {
                tagHelper = _tagHelperFactory.CreateTagHelper<ThumbnailTagHelper>(ViewContext);
            }
            else if (File.File.MediaType == MediaType.Video)
            {
                output.TagName = "video";
                tagHelper = _tagHelperFactory.CreateTagHelper<VideoTagHelper>(ViewContext);
            }
            else if (File.File.MediaType == MediaType.Audio)
            {
                output.TagName = "audio";
                tagHelper = _tagHelperFactory.CreateTagHelper<AudioTagHelper>(ViewContext);
            }

            if (tagHelper != null)
            {
                tagHelper.Init(context);

                tagHelper.Id = Id;
                tagHelper.File = File;
                tagHelper.FileId = FileId;
                tagHelper.Host = Host;
                tagHelper.Src = Src;
                tagHelper.Initialized = Initialized;

                if (tagHelper is BaseImageTagHelper imgTagHelper)
                {
                    imgTagHelper.Model = Model;
                    imgTagHelper.ImageQuery = ImageQuery;
                    imgTagHelper.AnchorPosition = AnchorPosition;
                    imgTagHelper.Height = Height;
                    imgTagHelper.NoFallback = NoFallback;
                    imgTagHelper.ResizeMode = ResizeMode;
                    imgTagHelper.Size = Size;
                    imgTagHelper.Width = Width;
                }
            }

            return tagHelper;
        }
    }
}