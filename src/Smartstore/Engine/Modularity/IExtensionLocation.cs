using Microsoft.Extensions.FileProviders;
using Smartstore.IO;

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
        /// The file provider that references the extension's root directory.
        /// This may point to the source code directory if running in dev mode,
        /// </summary>
        IFileSystem ContentRoot { get; }

        /// <summary>
        /// The file provider that points to the wwwroot directory of the extension.
        /// </summary>
        IFileProvider WebRoot { get; }
    }
}
