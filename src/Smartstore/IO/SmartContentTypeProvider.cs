using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.StaticFiles;

namespace Smartstore.IO
{
    public class SmartContentTypeProvider : IContentTypeProvider
    {
        private readonly FileExtensionContentTypeProvider _innerProvider;

        public SmartContentTypeProvider()
        {
            _innerProvider = new FileExtensionContentTypeProvider();
        }

        public SmartContentTypeProvider(FileExtensionContentTypeProvider innerProvider)
        {
            _innerProvider = Guard.NotNull(innerProvider);
        }

        /// <summary>
        /// The cross reference table of file extensions and content-types.
        /// </summary>
        public IDictionary<string, string> Mappings 
        { 
            get => _innerProvider.Mappings;
        }

        /// <summary>
        /// Gets the underlying <see cref="FileExtensionContentTypeProvider"/> instance used to determine MIME types
        /// based on file extensions.
        /// </summary>
        public FileExtensionContentTypeProvider InnerProvider
        {
            get => _innerProvider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetContentType(string subpath, out string contentType)
        {
            return _innerProvider.TryGetContentType(subpath, out contentType);
        }
    }
}
