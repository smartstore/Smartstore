using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;

namespace Smartstore.Engine
{
    public class RuntimeInfo
    {
        public RuntimeInfo(IHostEnvironment host)
        {
            // Use the current host and the process id as two servers could run on the same machine
            EnvironmentIdentifier = Environment.MachineName + '-' + Environment.ProcessId;

            // Use ContentRootPath as AppIdent
            ApplicationIdentifier = host.ContentRootPath;

            RID = GetRuntimeIdentifier();
            NativeLibraryDirectory = Path.Combine(AppContext.BaseDirectory, "runtimes", RID, "native");
        }

        internal static string GetRuntimeIdentifier()
        {
            var processArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win-" + processArchitecture;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux-" + processArchitecture;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx-" + processArchitecture;
            else
                throw new InvalidOperationException($"Unsupported OS Platform {RuntimeInformation.OSDescription}.");
        }

        /// <summary>
        /// Gets the current machines's name.
        /// </summary>
        public string MachineName { get; } = Environment.MachineName;

        /// <summary>
        /// Gets an application identifier which is unique across instances.
        /// </summary>
        public string ApplicationIdentifier { get; }

        /// <summary>
        /// Gets a unique environment (runtime instance) identifier.
        /// </summary>
        public string EnvironmentIdentifier { get; }

        /// <summary>
        /// Gets the full path to the entry assembly directory.
        /// </summary>
        public string BaseDirectory { get; } = AppContext.BaseDirectory;

        /// <summary>
        /// Gets the full path to the native library directory, e.g. "runtimes\win-x64\native".
        /// </summary>
        public string NativeLibraryDirectory { get; }

        /// <summary>
        /// Gets the description of the operating system.
        /// </summary>
        public string OSDescription { get; } = RuntimeInformation.OSDescription;

        /// <summary>
        /// Indicates whether the current application is running on the Windows platform.
        /// </summary>
        public bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Gets the process architecture.
        /// </summary>
        public Architecture ProcessArchitecture { get; } = RuntimeInformation.ProcessArchitecture;

        /// <summary>
        /// Gets the version agnostic runtime identifier (RID), e.g. win-x64, linux-x64, osx-x64 etc.
        /// </summary>
        public string RID { get; }
    }
}
