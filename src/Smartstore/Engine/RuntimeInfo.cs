using System;
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

            var processArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLower();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                RID = "win-" + processArchitecture;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                RID = "linux-" + processArchitecture;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                RID = "osx-" + processArchitecture;
            else
                throw new InvalidOperationException($"Unsupported OS Platform {RuntimeInformation.OSDescription}.");

            NativeLibraryDirectory = $"{AppContext.BaseDirectory}runtimes\\{RID}\\native\\";
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
        /// Gets the process architecture.
        /// </summary>
        public Architecture ProcessArchitecture { get; } = RuntimeInformation.ProcessArchitecture;

        /// <summary>
        /// Gets the version agnostic runtime identifier (RID), e.g. win-x64, linux-x64, osx-x64 etc.
        /// </summary>
        public string RID { get; }
    }
}
