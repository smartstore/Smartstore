using System;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.IO
{
    /// <inheritdoc/>
    public interface IFileEntry : IFileInfo
    {
        /// <summary>
        /// The path relative to the storage root
        /// </summary>
        string SubPath { get; }
    }
}