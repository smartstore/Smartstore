using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine.Initialization;

namespace Smartstore.Engine.Runtimes
{
    /// <summary>
    /// Ensures that globally required native library files exist.
    /// Downloads, extracts and installs them if not.
    /// </summary>
    internal class NativeLibraryInitializer : IApplicationInitializer
    {
        public int Order => int.MinValue;

        public bool ThrowOnError => true;

        public int MaxAttempts => 1;

        public Task OnFailAsync(Exception exception, bool willRetry) => Task.CompletedTask;

        public async Task InitializeAsync(HttpContext httpContext)
        {
            var appContext = EngineContext.Current.Application;
            var rid = appContext.RuntimeInfo.RID;
            var libraryManager = httpContext.RequestServices.GetRequiredService<INativeLibraryManager>();

            // We must deploy the ClearScript.V8 native library very early in the app lifecycle
            // because we have no control about ClearScript.

            var libRequests = new[]
            {
                new InstallNativePackageRequest($"ClearScriptV8.{rid}", false, $"Microsoft.ClearScript.V8.Native.{rid}") 
                { 
                    AppendRIDToPackageId = false,
                    MinVersion = "7.1.7"
                }
            };

            using var libraryInstaller = libraryManager.CreateLibraryInstaller();

            foreach (var libRequest in libRequests)
            {
                var fi = libRequest.IsExecutable 
                    ? libraryManager.GetNativeExecutable(libRequest.LibraryName, libRequest.MinVersion, libRequest.MaxVersion)
                    : libraryManager.GetNativeLibrary(libRequest.LibraryName, libRequest.MinVersion, libRequest.MaxVersion);

                if (!fi.Exists)
                {
                    await libraryInstaller.InstallFromPackageAsync(libRequest);
                }
            }
        }
    }
}
