using System;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.Engine.Modularity
{
    public interface IExtensionLocation
    {
        /// <summary>
        /// Absolute application path to extension directory, e.g. "/Modules/MyModule/".
        /// </summary>
        string Path { get; }

        /// <summary>
        /// The full physical path of the extension directory.
        /// </summary>
        string PhysicalPath { get; }

        /// <summary>
        /// The file provider that points to the wwwroot directory of the extension.
        /// </summary>
        IFileProvider WebFileProvider { get; }
    }
}
