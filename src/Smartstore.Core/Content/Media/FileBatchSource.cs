﻿#nullable enable

using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Net.Http;

namespace Smartstore.Core.Content.Media
{
    public class FileBatchSource(MediaStorageItem source) : Disposable
    {

        /// <summary>
        /// The file source as <see cref="MediaStorageItem"/>.
        /// </summary>
        public MediaStorageItem Source { get; } = Guard.NotNull(source);

        /// <summary>
        /// Name of file including extension.
        /// </summary>
        public string FileName { get; init; } = default!;

        /// <summary>
        /// Any state to identify the source later after batch save. E.g.: <see cref="ImportRow{T}"/>, <see cref="DownloadManagerItem"/> etc.
        /// </summary>
        public object? State { get; init; }

        protected override void OnDispose(bool disposing)
        {
            if (disposing) Source.Dispose();
        }
    }

    public class FileBatchResult
    {
        public FileBatchSource Source { get; init; } = default!;
        public MediaFileInfo? File { get; init; }
        public MediaPathData? PathData { get; init; }
        public Exception? Exception { get; set; }
        public bool IsDuplicate { get; set; }
        public string? UniquePath { get; set; }
        internal MediaStorageItem? StorageItem { get; set; }
    }
}
