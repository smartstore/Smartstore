using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Widgets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Web.TagHelpers.Shared
{
    // TODO: (ms) (core) Make model class, no interface.
    public interface IFileUploaderModel
    {
        public AttributeDictionary HtmlAttributes { get; set; }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string UploadUrl { get; set; }
        public string UploadText { get; set; }

        public bool DisplayRemoveButton { get; set; }
        public bool DisplayRemoveButtonAfterUpload { get; set; }
        public bool DisplayBrowseMediaButton { get; set; }

        public bool HasTemplatePreview { get; set; }
        public bool DownloadEnabled { get; set; }
        public bool MultiFile { get; set; }
        public string TypeFilter { get; set; }

        public string PreviewContainerId { get; set; }
        public int? MainFileId { get; set; }
        public long? MaxFileSize { get; set; }

        public string EntityType { get; set; }
        public int EntityId { get; set; }

        public string DeleteUrl { get; set; }
        public string SortUrl { get; set; }
        public string EntityAssignmentUrl { get; set; }

        public IEnumerable<IMediaFile> UploadedFiles { get; set; }

        public string OnUploading { get; set; }
        public string OnUploadCompleted { get; set; }
        public string OnError { get; set; }
        public string OnFileRemoved { get; set; }
        public string OnAborted { get; set; }
        public string OnCompleted { get; set; }
        public string OnMediaSelected { get; set; }
    }

    [HtmlTargetElement("file-uploader", Attributes = UploadUrlAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class FileUploaderTagHelper : SmartTagHelper, IFileUploaderModel
    {
        const string FileUploaderAttributeName = "sm-file-uploader-name";
        const string UploadUrlAttributeName = "sm-upload-url";
        const string DisplayBrowseMediaButtonAttributeName = "sm-display-browse-media-button";
        const string TypeFilterAttributeName = "sm-type-filter";
        const string DisplayRemoveButtonAttributeName = "sm-display-remove-button";
        const string DisplayRemoveButtonAfterUploadAttributeName = "sm-display-remove-button-after-upload";
        const string UploadTextAttributeName = "sm-upload-text";
        const string OnUploadingAttributeName = "sm-on-uploading";
        const string OnUploadCompletedAttributeName = "sm-on-upload-completed";
        const string OnErrorAttributeName = "sm-on-error";
        const string OnFileRemovedAttributeName = "sm-on-file-removed";
        const string OnAbortedAttributeName = "sm-on-aborted";
        const string OnCompletedAttributeName = "sm-on-completed";
        const string OnMediaSelectedAttributeName = "sm-on-media-selected";
        const string MultiFileAttributeName = "sm-multi-file";
        const string HasTemplatePreviewAttributeName = "sm-has-template-preview";
        const string DownloadEnabledAttributeName = "sm-download-enabled";
        const string MaxFileSizeAttributeName = "sm-max-file-size";
        const string MediaPathAttributeName = "sm-media-path";
        const string PreviewContainerIdAttributeName = "sm-preview-container-id";
        const string MainFileIdAttributeName = "sm-main-file-id";
        const string UploadedFilesAttributeName = "sm-uploaded-files";
        const string EntityTypeAttributeName = "sm-entity-type";
        const string EntityIdAttributeName = "sm-entity-id";
        const string DeleteUrlAttributeName = "sm-delete-url";
        const string SortUrlAttributeName = "sm-sort-url";
        const string EntityAssignmentUrlAttributeName = "sm-entity-assigment-url";

        private readonly IMediaTypeResolver _mediaTypeResolver;

        public FileUploaderTagHelper(IMediaTypeResolver mediaTypeResolver)
        {
            _mediaTypeResolver = mediaTypeResolver;
        }

        public override int Order => 100;

        // TODO: (ms) (core) Remove and refactor! WTF!!!
        [HtmlAttributeNotBound]
        public AttributeDictionary HtmlAttributes { get; set; } = new();

        // TODO: (ms) (core) Id is already used by SmartTagHelper. Rename to ControlId or somenthing like that.
        // TODO: (ms) (core) (from mc) Don't ever use Id prop with SmartTagHelper. Remove this and HtmlAttribute and all callers completely!
        [HtmlAttributeNotBound]
        public string Id
        {
            get => !HtmlAttributes.ContainsKey("id") ? Name : HtmlAttributes["id"];
            set => HtmlAttributes["id"] = value;
        }

        [HtmlAttributeName(FileUploaderAttributeName)]
        public string Name { get; set; }

        [HtmlAttributeName(MediaPathAttributeName)]
        public string Path { get; set; } = SystemAlbumProvider.Files;

        [HtmlAttributeName(UploadUrlAttributeName)]
        public string UploadUrl
        {
            get => HtmlAttributes["data-upload-url"];
            set => HtmlAttributes["data-upload-url"] = value;
        }

        [HtmlAttributeName(UploadTextAttributeName)]
        public string UploadText { get; set; }

        [HtmlAttributeName(DisplayRemoveButtonAttributeName)]
        public bool DisplayRemoveButton { get; set; }

        [HtmlAttributeName(DisplayRemoveButtonAfterUploadAttributeName)]
        public bool DisplayRemoveButtonAfterUpload { get; set; }

        [HtmlAttributeName(DisplayBrowseMediaButtonAttributeName)]
        public bool DisplayBrowseMediaButton { get; set; }

        [HtmlAttributeName(HasTemplatePreviewAttributeName)]
        public bool HasTemplatePreview { get; set; }

        [HtmlAttributeName(DownloadEnabledAttributeName)]
        public bool DownloadEnabled { get; set; }

        [HtmlAttributeName(MultiFileAttributeName)]
        public bool MultiFile { get; set; }

        [HtmlAttributeName(TypeFilterAttributeName)]
        public string TypeFilter { get; set; }

        [HtmlAttributeName(PreviewContainerIdAttributeName)]
        public string PreviewContainerId { get; set; }

        [HtmlAttributeName(MainFileIdAttributeName)]
        public int? MainFileId { get; set; }

        [HtmlAttributeName(MaxFileSizeAttributeName)]
        public long? MaxFileSize { get; set; }

        [HtmlAttributeName(EntityTypeAttributeName)]
        public string EntityType { get; set; }

        [HtmlAttributeName(EntityIdAttributeName)]
        public int EntityId { get; set; }

        [HtmlAttributeName(DeleteUrlAttributeName)]
        public string DeleteUrl { get; set; }

        [HtmlAttributeName(SortUrlAttributeName)]
        public string SortUrl { get; set; }

        [HtmlAttributeName(EntityAssignmentUrlAttributeName)]
        public string EntityAssignmentUrl { get; set; }

        [HtmlAttributeName(UploadedFilesAttributeName)]
        public IEnumerable<IMediaFile> UploadedFiles { get; set; }

        [HtmlAttributeName(OnUploadingAttributeName)]
        public string OnUploading { get; set; }

        [HtmlAttributeName(OnUploadCompletedAttributeName)]
        public string OnUploadCompleted { get; set; }

        [HtmlAttributeName(OnErrorAttributeName)]
        public string OnError { get; set; }

        [HtmlAttributeName(OnFileRemovedAttributeName)]
        public string OnFileRemoved { get; set; }

        [HtmlAttributeName(OnAbortedAttributeName)]
        public string OnAborted { get; set; }

        [HtmlAttributeName(OnCompletedAttributeName)]
        public string OnCompleted { get; set; }

        [HtmlAttributeName(OnMediaSelectedAttributeName)]
        public string OnMediaSelected { get; set; }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            var extensions = _mediaTypeResolver.ParseTypeFilter(TypeFilter.HasValue() ? TypeFilter : "*");
            HtmlAttributes["data-accept"] = "." + string.Join(",.", extensions);
            HtmlAttributes["data-show-remove-after-upload"] = DisplayRemoveButtonAfterUpload.ToString().ToLower();

            var widget = new ComponentWidgetInvoker("FileUploader", this);
            var partial = await widget.InvokeAsync(ViewContext);

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;
            output.Content.SetHtmlContent(partial);
        }
    }
}