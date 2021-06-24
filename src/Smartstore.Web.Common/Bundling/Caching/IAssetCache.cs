using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Web.Bundling
{
    /// <summary>
    /// A file system based caching mechanism for dynamically translated assets like Sass etc.
    /// </summary>
    public interface IAssetCache
    {
        Task<CachedAssetEntry> GetAssetAsync(string route);

        Task<CachedAssetEntry> InsertAssetAsync(string route, IEnumerable<string> includedFiles, string content, params string[] processorCodes);

        Task<bool> InvalidateAssetAsync(string route);

        Task ClearAsync();
    }
}
