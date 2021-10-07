using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Razor.Language;

namespace Smartstore.Web.Razor.RuntimeCompilation
{
    internal static class ChecksumValidator
    {
        public static bool IsRecompilationSupported(RazorCompiledItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // A Razor item only supports recompilation if its primary source file has a checksum.
            //
            // Other files (view imports) may or may not have existed at the time of compilation,
            // so we may not have checksums for them.
            var checksums = item.GetChecksumMetadata();
            return checksums.Any(c => string.Equals(item.Identifier, c.Identifier, StringComparison.OrdinalIgnoreCase));
        }

        // Validates that we can use an existing precompiled view by comparing checksums with files on
        // disk.
        public static bool IsItemValid(RazorProjectFileSystem fileSystem, RazorCompiledItem item)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var checksums = item.GetChecksumMetadata();

            // The checksum that matches 'Item.Identity' in this list is significant. That represents the main file.
            //
            // We don't really care about the validation unless the main file exists. This is because we expect
            // most sites to have some _ViewImports in common location. That means that in the case you're
            // using views from a 3rd party library, you'll always have **some** conflicts.
            //
            // The presence of the main file with the same content is a very strong signal that you're in a
            // development scenario.
            var primaryChecksum = checksums
                .FirstOrDefault(c => string.Equals(item.Identifier, c.Identifier, StringComparison.OrdinalIgnoreCase));
            if (primaryChecksum == null)
            {
                // No primary checksum, assume valid.
                return true;
            }

            var projectItem = fileSystem.GetItem(primaryChecksum.Identifier, fileKind: null);
            if (!projectItem.Exists)
            {
                // Main file doesn't exist - assume valid.
                return true;
            }

            var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);
            //var sourceDocument = ReadSourceDocument(projectItem);
            if (!string.Equals(sourceDocument.GetChecksumAlgorithm(), primaryChecksum.ChecksumAlgorithm) ||
                !ChecksumsEqual(primaryChecksum.Checksum, sourceDocument.GetChecksum()))
            {
                // Main file exists, but checksums not equal.
                return false;
            }
            
            for (var i = 0; i < checksums.Count; i++)
            {
                var checksum = checksums[i];
                if (string.Equals(item.Identifier, checksum.Identifier, StringComparison.OrdinalIgnoreCase))
                {
                    // Ignore primary checksum on this pass.
                    continue;
                }

                var importItem = fileSystem.GetItem(checksum.Identifier, fileKind: null);
                if (!importItem.Exists)
                {
                    // Import file doesn't exist - assume invalid.
                    return false;
                }

                sourceDocument = RazorSourceDocument.ReadFrom(importItem);
                //sourceDocument = ReadSourceDocument(projectItem);
                if (!string.Equals(sourceDocument.GetChecksumAlgorithm(), checksum.ChecksumAlgorithm) ||
                    !ChecksumsEqual(checksum.Checksum, sourceDocument.GetChecksum()))
                {
                    // Import file exists, but checksums not equal.
                    return false;
                }
            }

            return true;
        }

        private static bool ChecksumsEqual(string checksum, byte[] bytes)
        {
            if (bytes.Length * 2 != checksum.Length)
            {
                return false;
            }

            for (var i = 0; i < bytes.Length; i++)
            {
                var text = bytes[i].ToString("x2", CultureInfo.InvariantCulture);
                if (checksum[i * 2] != text[0] || checksum[i * 2 + 1] != text[1])
                {
                    return false;
                }
            }

            return true;
        }

        private static RazorSourceDocument ReadSourceDocument(RazorProjectItem projectItem)
        {
            if (projectItem == null)
            {
                throw new ArgumentNullException(nameof(projectItem));
            }

            // ProjectItem.PhysicalPath is usually an absolute (rooted) path.
            var filePath = projectItem.PhysicalPath;
            if (string.IsNullOrEmpty(filePath))
            {
                // Fall back to the relative path only if necessary.
                filePath = projectItem.RelativePhysicalPath;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                // Then fall back to the FilePath (yeah it's a bad name) which is like an MVC view engine path
                // It's much better to have something than nothing.
                filePath = projectItem.FilePath;
            }

            //if (projectItem.RazorSourceDocument is not null)
            //{
            //    return projectItem.RazorSourceDocument;
            //}

            using (var stream = projectItem.Read())
            {
                // Autodetect the encoding.
                var relativePath = projectItem.RelativePhysicalPath ?? projectItem.FilePath;

                var streamLength = (int)stream.Length;
                var content = string.Empty;
                var contentEncoding = Encoding.UTF8;

                if (streamLength > 0)
                {
                    var bufferSize = Math.Min(streamLength, 40 * 1024);

                    var reader = new StreamReader(
                        stream,
                        contentEncoding,
                        detectEncodingFromByteOrderMarks: true,
                        bufferSize: bufferSize,
                        leaveOpen: true);

                    using (reader)
                    {
                        reader.Peek();      // Just to populate the encoding
                        content = reader.ReadToEnd();
                    }
                }

                return new StringSourceDocument(content, contentEncoding, new RazorSourceDocumentProperties(filePath, relativePath));
            }
        }
    }
}
