namespace Smartstore.WebApi.Client.Models
{
    public enum DuplicateFileHandling
    {
        ThrowError,
        Overwrite,
        Rename
    }


    /// <summary>
    /// Represents file upload data to be used for multipart form data.
    /// </summary>
    [Serializable]
    public class FileUploadModel
    {
        /// <summary>
        /// Entity identifier to which the files belong.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Paths of files to upload.
        /// </summary>
        public List<FileModel> Files { get; set; } = new();

        /// <summary>
        /// Any custom properties to be added to multipart form data. Examples:
        /// Identify product by SKU: Sku = "my SKU"
        /// Delete existing import files: deleteExisting = true
        /// Start import: startImport = true
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        [Serializable]
        public class FileModel
        {
            /// <summary>
            /// File identifier for updating files.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Absolute local path of file to be uploaded.
            /// </summary>
            public string LocalPath { get; set; }

            /// <summary>
            /// Media service path in shop.
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// Whether the file in shop is marked as transient.
            /// </summary>
            public bool IsTransient { get; set; } = true;

            /// <summary>
            /// Duplicate file handling.
            /// </summary>
            public DuplicateFileHandling DuplicateFileHandling { get; set; } = DuplicateFileHandling.ThrowError;
        }
    }
}
