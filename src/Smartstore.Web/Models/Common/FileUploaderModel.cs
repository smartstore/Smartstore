using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.TagHelpers.Shared;
using System.Collections.Generic;

namespace Smartstore.Web.Models.Common
{
    public partial class FileUploaderModel : IFileUploaderModel
    {
        public AttributeDictionary HtmlAttributes { get; set; } = new();

        public string Id
        {
            get => !HtmlAttributes.ContainsKey("id") ? Name : HtmlAttributes["id"];
            set => HtmlAttributes["id"] = value;
        }

        public string Name { get; set; }
        public string Path { get; set; }

        public string UploadUrl
        {
            get => HtmlAttributes["data-upload-url"];
            set => HtmlAttributes["data-upload-url"] = value;
        }

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

        // TODO: (core) (ms) Check usage.
        public string ClickableElement { get; set; }

        public FileUploaderModel(IFileUploaderModel model)
        {
            MiniMapper.Map(model, this);
        }
    }
}