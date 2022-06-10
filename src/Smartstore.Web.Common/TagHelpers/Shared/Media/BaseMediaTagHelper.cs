using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.TagHelpers.Shared
{
    public abstract class BaseMediaTagHelper : SmartTagHelper
    {
        protected const string FileAttributeName = "sm-file";
        protected const string FileIdAttributeName = "sm-file-id";
        protected const string HostAttributeName = "sm-url-host";

        public override void Init(TagHelperContext context)
        {
            base.Init(context);

            MediaService = ViewContext.HttpContext.GetServiceScope().Resolve<IMediaService>();
            MediaUrlGenerator = ViewContext.HttpContext.GetServiceScope().Resolve<IMediaUrlGenerator>();
        }

        [HtmlAttributeNotBound]
        protected IMediaService MediaService { get; set; }

        [HtmlAttributeNotBound]
        protected IMediaUrlGenerator MediaUrlGenerator { get; set; }

        [HtmlAttributeNotBound]
        internal bool Initialized { get; set; }

        [HtmlAttributeNotBound]
        protected internal string Src { get; set; }

        /// <summary>
        /// The <see cref="MediaFileInfo"/> instance to render a tag for.
        /// </summary>
        [HtmlAttributeName(FileAttributeName)]
        public MediaFileInfo File { get; set; }

        /// <summary>
        /// The unique identifier of the media file to render a tag for.
        /// </summary>
        [HtmlAttributeName(FileIdAttributeName)]
        public int? FileId { get; set; }

        /// <summary>
        /// Store host for an absolute URL that also contains scheme and host parts. 
        /// <c>Omitting</c> this attribute tries to resolve host automatically based on <see cref="Store.ContentDeliveryNetwork"/> or <see cref="MediaSettings.AutoGenerateAbsoluteUrls"/>.
        /// <c>Empty</c> attribute value bypasses automatic host resolution and does NOT prepend host to path.
        /// <c>Any string</c> value: host name to use explicitly.
        /// </summary>
        [HtmlAttributeName(HostAttributeName)]
        public string Host { get; set; }

        protected override sealed async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!Initialized)
            {
                await PrepareModelAsync();
                Src = GenerateMediaUrl();
                Initialized = true;
            }

            await ProcessMediaAsync(context, output);
        }

        protected override sealed void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (!Initialized)
            {
                PrepareModelAsync().GetAwaiter().GetResult();
                Src = GenerateMediaUrl();
                Initialized = true;
            }

            ProcessMedia(context, output);
        }

        protected virtual async Task PrepareModelAsync()
        {
            File ??= await MediaService.GetFileByIdAsync(FileId ?? 0, MediaLoadFlags.AsNoTracking);
        }

        protected virtual Task ProcessMediaAsync(TagHelperContext context, TagHelperOutput output)
        {
            ProcessMedia(context, output);
            return Task.CompletedTask;
        }

        protected abstract void ProcessMedia(TagHelperContext context, TagHelperOutput output);

        protected virtual string GenerateMediaUrl()
        {
            return MediaUrlGenerator.GenerateUrl(File, QueryString.Empty, Host, false);
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
