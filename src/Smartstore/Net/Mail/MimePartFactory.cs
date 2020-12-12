using System;
using System.IO;
using MimeKit;
using Smartstore.IO;

namespace Smartstore.Net.Mail
{
    public static class MimePartFactory
    {
        /// <summary>
        /// Creates a mime attachment for a mail message from an <see cref="IFile"/>.
        /// </summary>
        public static MimePart CreateMimePart(IFile file)
        {
            Guard.NotNull(file, nameof(file));

            return CreateMimePart(
                file.OpenRead(),
                file.Name,
                null,
                file.LastModified,
                file.LastModified);
        }

        /// <summary>
        /// Creates a mime attachment for a mail message from a <see cref="FileInfo"/> instance.
        /// </summary>
        public static MimePart CreateMimePart(FileInfo fileInfo)
        {
            Guard.NotNull(fileInfo, nameof(fileInfo));

            return CreateMimePart(
                fileInfo.OpenRead(),
                fileInfo.Name,
                null,
                fileInfo.CreationTimeUtc,
                fileInfo.LastWriteTimeUtc,
                fileInfo.LastAccessTimeUtc);
        }

        /// <summary>
        /// Creates a mime attachment for a mail message from a stream.
        /// </summary>
        /// <param name="contentStream">Attachment input stream</param>
        /// <param name="name">Attachment file name.</param>
        /// <param name="contentType">Attachment content type. Pass <c>null</c> to auto-resolve from <paramref name="name"/>.</param>
        /// <param name="creationDate">CreationDate content disposition entry</param>
        /// <param name="modifiedDate">ModificationDate content disposition entry</param>
        /// <param name="readDate">ReadDate content disposition entry</param>
        public static MimePart CreateMimePart(
            Stream contentStream, 
            string name,
            string contentType = null,
            DateTimeOffset? creationDate = null,
            DateTimeOffset? modificationDate = null,
            DateTimeOffset? readDate = null)
        {
            Guard.NotNull(contentStream, nameof(contentStream));
            Guard.NotEmpty(name, nameof(name));
            Guard.NotEmpty(contentType, nameof(contentType));

            if (!ContentType.TryParse(contentType.EmptyNull() ?? MimeTypes.GetMimeType(name), out var mimeContentType))
            {
                mimeContentType = new ContentType("application", "octet-stream");
            } 

            return new MimePart(mimeContentType)
            {
                FileName = name,
                Content = new MimeContent(contentStream, ContentEncoding.Default),
                ContentDisposition = new ContentDisposition
                {
                    CreationDate = creationDate,
                    ModificationDate = modificationDate,
                    ReadDate = readDate
                }
            };
        }
    }
}
