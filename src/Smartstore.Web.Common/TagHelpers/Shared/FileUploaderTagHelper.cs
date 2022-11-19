using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Content.Media;

namespace Smartstore.Web.TagHelpers.Shared
{
    public class FileUploaderModel
    {
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
        public string AcceptedFileExtensions { get; set; }

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

        public string ClickableElement { get; set; }
    }

    [HtmlTargetElement("file-uploader", Attributes = UploadUrlAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class FileUploaderTagHelper : SmartTagHelper
    {
        const string FileUploaderAttributeName = "file-uploader-name";
        const string UploadUrlAttributeName = "upload-url";
        const string DisplayBrowseMediaButtonAttributeName = "display-browse-media-button";
        const string TypeFilterAttributeName = "type-filter";
        const string DisplayRemoveButtonAttributeName = "display-remove-button";
        const string DisplayRemoveButtonAfterUploadAttributeName = "display-remove-button-after-upload";
        const string UploadTextAttributeName = "upload-text";
        const string OnUploadingAttributeName = "onuploading";
        const string OnUploadCompletedAttributeName = "onuploadcompleted";
        const string OnErrorAttributeName = "onerror";
        const string OnFileRemovedAttributeName = "onfileremoved";
        const string OnAbortedAttributeName = "onaborted";
        const string OnCompletedAttributeName = "oncompleted";
        const string OnMediaSelectedAttributeName = "onmediaselected";
        const string MultiFileAttributeName = "multi-file";
        const string HasTemplatePreviewAttributeName = "has-template-preview";
        const string DownloadEnabledAttributeName = "download-enabled";
        const string MaxFileSizeAttributeName = "max-file-size";
        const string MediaPathAttributeName = "media-path";
        const string PreviewContainerIdAttributeName = "preview-container-id";
        const string MainFileIdAttributeName = "main-file-id";
        const string UploadedFilesAttributeName = "uploaded-files";
        const string EntityTypeAttributeName = "entity-type";
        const string EntityIdAttributeName = "entity-id";
        const string DeleteUrlAttributeName = "delete-url";
        const string SortUrlAttributeName = "sort-url";
        const string EntityAssignmentUrlAttributeName = "entity-assigment-url";

        private readonly IMediaTypeResolver _mediaTypeResolver;

        public FileUploaderTagHelper(IMediaTypeResolver mediaTypeResolver)
        {
            _mediaTypeResolver = mediaTypeResolver;
        }

        public override int Order => 100;

        [HtmlAttributeName(FileUploaderAttributeName)]
        public string Name { get; set; }

        [HtmlAttributeName(MediaPathAttributeName)]
        public string Path { get; set; } = SystemAlbumProvider.Files;

        [HtmlAttributeName(UploadUrlAttributeName)]
        public string UploadUrl { get; set; }

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
            var model = new FileUploaderModel
            {
                Name = Name,
                Path = Path,
                UploadUrl = UploadUrl,
                UploadText = UploadText,
                DisplayRemoveButton = DisplayRemoveButton,
                DisplayBrowseMediaButton = DisplayBrowseMediaButton,
                DisplayRemoveButtonAfterUpload = DisplayRemoveButtonAfterUpload,
                HasTemplatePreview = HasTemplatePreview,
                DownloadEnabled = DownloadEnabled,
                MultiFile = MultiFile,
                TypeFilter = TypeFilter,
                PreviewContainerId = PreviewContainerId,
                MainFileId = MainFileId,
                MaxFileSize = MaxFileSize,
                EntityType = EntityType,
                EntityId = EntityId,
                DeleteUrl = DeleteUrl,
                SortUrl = SortUrl,
                EntityAssignmentUrl = EntityAssignmentUrl,
                UploadedFiles = UploadedFiles,
                OnUploading = OnUploading,
                OnUploadCompleted = OnUploadCompleted,
                OnError = OnError,
                OnFileRemoved = OnFileRemoved,
                OnAborted = OnAborted,
                OnCompleted = OnCompleted,
                OnMediaSelected = OnMediaSelected
            };

            var extensions = _mediaTypeResolver.ParseTypeFilter(TypeFilter.HasValue() ? TypeFilter : "*");
            model.AcceptedFileExtensions = "." + string.Join(",.", extensions);

            var widget = new ComponentWidget("FileUploader", new { model });
            var partial = await widget.InvokeAsync(ViewContext);

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;
            output.Content.SetHtmlContent(partial);
        }
    }
}