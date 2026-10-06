#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Caches SVG payloads under canonical-address:revision keys. Revisions are not part of persistent icon addresses.
/// </summary>
public interface IIconCache
{
    /// <summary>
    /// Gets a detached cached payload, or null.
    /// </summary>
    /// <param name="key">A fully qualified canonical address followed by a colon and its hexadecimal content revision.</param>
    /// <returns>A payload owned by the caller, or null on a cache miss.</returns>
    Task<IconSvg?> GetAsync(string key, CancellationToken cancelToken = default);

    /// <summary>
    /// Stores a detached payload with a bounded lifetime.
    /// </summary>
    /// <param name="key">A fully qualified canonical address followed by a colon and its hexadecimal content revision.</param>
    /// <param name="svg">The prepared payload to copy into the cache. Subsequent caller changes must not affect the cached value.</param>
    Task PutAsync(string key, IconSvg svg, CancellationToken cancelToken = default);

    /// <summary>
    /// Removes a particular address and revision.
    /// </summary>
    /// <param name="key">The exact canonical-address:revision key to remove.</param>
    Task RemoveAsync(string key, CancellationToken cancelToken = default);

    /// <summary>
    /// Removes cached revisions of a library by its canonical library name.
    /// </summary>
    /// <param name="libraryName">The selector used in canonical addresses: the library's short name when configured, otherwise its system name.</param>
    Task InvalidateLibraryAsync(string libraryName, CancellationToken cancelToken = default);
}
