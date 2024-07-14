﻿using Smartstore.Core.Localization;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    #region Exception classes

    public sealed class MediaFileNotFoundException : FileNotFoundException
    {
        public MediaFileNotFoundException(string message) : base(message) { }
    }

    public sealed class MediaFolderNotFoundException : DirectoryNotFoundException
    {
        public MediaFolderNotFoundException(string message) : base(message) { }
    }

    public sealed class DuplicateMediaFileException : Exception
    {
        public DuplicateMediaFileException(string message, MediaFileInfo dupeFile, string uniquePath) : base(message)
        {
            File = dupeFile;
            UniquePath = uniquePath;
        }

        public MediaFileInfo File { get; }
        public string UniquePath { get; }
    }

    public sealed class DuplicateMediaFolderException : Exception
    {
        public DuplicateMediaFolderException(string message, MediaFolderNode dupeFolder) : base(message)
        {
            Folder = dupeFolder;
        }

        public MediaFolderNode Folder { get; }
    }

    public sealed class NotSameAlbumException : Exception
    {
        public NotSameAlbumException(string message) : base(message) { }
    }

    public sealed class DeniedMediaTypeException : Exception
    {
        public DeniedMediaTypeException(string message) : base(message) { }
    }

    public sealed class ExtractThumbnailException : Exception
    {
        public ExtractThumbnailException(string message) : base(message) { }
        public ExtractThumbnailException(string message, Exception innerException) : base(message, innerException) { }
    }

    public sealed class MaxMediaFileSizeExceededException : Exception
    {
        public MaxMediaFileSizeExceededException(string message) : base(message) { }
    }

    public sealed class DeleteTrackedFileException : Exception
    {
        public DeleteTrackedFileException(string message, MediaFile file, Exception innerException) : base(message, innerException)
        {
            File = file;
        }

        public MediaFile File { get; }
    }

    #endregion

    public class MediaExceptionFactory
    {
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public MediaFileNotFoundException FileNotFound(string path) => new MediaFileNotFoundException(T("Admin.Media.Exception.FileNotFound", path));

        public MediaFolderNotFoundException FolderNotFound(string path) => new MediaFolderNotFoundException(T("Admin.Media.Exception.FolderNotFound", path));

        public DuplicateMediaFileException DuplicateFile(string fullPath, MediaFileInfo dupeFile, string uniquePath) => new DuplicateMediaFileException(T("Admin.Media.Exception.DuplicateFile", fullPath), dupeFile, uniquePath);

        public DuplicateMediaFolderException DuplicateFolder(string fullPath, MediaFolderNode dupeFolder)
        => new DuplicateMediaFolderException(T("Admin.Media.Exception.DuplicateFolder", fullPath), dupeFolder);

        public NotSameAlbumException NotSameAlbum(string sourcePath, string destPath)
        => new NotSameAlbumException(T("Admin.Media.Exception.NotSameAlbum", sourcePath, destPath));

        public DeniedMediaTypeException DeniedMediaType(string fileName, string currentType, string[] acceptedTypes = null)
        {
            var msg = T("Admin.Media.Exception.DeniedMediaType", fileName, currentType);
            if (acceptedTypes != null && acceptedTypes.Length > 0)
            {
                var types = string.Join(", ", acceptedTypes);
                msg += T("Admin.Media.Exception.DeniedMediaType.Hint", types, currentType);
            }

            return new DeniedMediaTypeException(msg);
        }

        public ExtractThumbnailException ExtractThumbnail(string path, string reason = null)
        => new ExtractThumbnailException(T("Admin.Media.Exception.ExtractThumbnail", path, reason.NaIfEmpty()));

        public ExtractThumbnailException ExtractThumbnail(string path, Exception innerException)
        {
            Guard.NotNull(innerException, nameof(innerException));
            return new ExtractThumbnailException(T("Admin.Media.Exception.ExtractThumbnail", path, innerException.Message), innerException);
        }

        public MaxMediaFileSizeExceededException MaxFileSizeExceeded(string fileName, long fileSize, long maxSize)
        => new MaxMediaFileSizeExceededException(T("Admin.Media.Exception.MaxFileSizeExceeded", fileName.NaIfEmpty(), Prettifier.HumanizeBytes(fileSize), Prettifier.HumanizeBytes(maxSize)));

        public DeleteTrackedFileException DeleteTrackedFile(MediaFile file, Exception innerException)
        {
            Guard.NotNull(file, nameof(file));
            return new DeleteTrackedFileException(T("Admin.Media.Exception.DeleteReferenzedFile", file.Name), file, innerException);
        }

        public InvalidOperationException IdenticalPaths(MediaFileInfo file)
        {
            Guard.NotNull(file, nameof(file));
            return new InvalidOperationException(T("Admin.Media.Exception.FileNamesIdentical", file.Path));
        }
    }
}