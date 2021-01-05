using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Manages all registered albums.
    /// </summary>
    public interface IAlbumRegistry
    {
        /// <summary>
        /// Enlists all albums.
        /// </summary>
        Task<IReadOnlyCollection<AlbumInfo>> GetAllAlbumsAsync();

        /// <summary>
        /// Gets the names of all albums.
        /// </summary>
        /// <param name="withTrackDetectors">If <c>true</c> skips all albums without track detection capability.</param>
        Task<IEnumerable<string>> GetAlbumNamesAsync(bool withTrackDetectors = false);

        /// <summary>
        /// Gets an album by name.
        /// </summary>
        /// <param name="name">Name of album to retrieve.</param>
        Task<AlbumInfo> GetAlbumByNameAsync(string name);

        /// <summary>
        /// Gets an album by ID.
        /// </summary>
        /// <param name="name">Storage id of album to retrieve.</param>
        Task<AlbumInfo> GetAlbumByIdAsync(int id);

        /// <summary>
        /// Deletes album and all containing files
        /// </summary>
        /// <param name="albumName">Name of album to delete</param>
        Task UninstallAlbumAsync(string albumName);

        /// <summary>
        /// Deletes album but keeps containing files
        /// </summary>
        /// <param name="albumName">Name of album to delete</param>
        /// <param name="moveFilesToAlbum">Name of album to move files to</param>
        Task DeleteAlbumAsync(string albumName, string moveFilesToAlbum);

        /// <summary>
        /// Clears album cache and reloads all entities from database.
        /// </summary>
        Task ClearCacheAsync();
    }
}
