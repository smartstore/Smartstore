# Native libraries

Some Smartstore features rely on OS‑specific binaries such as PDF engines or image processors. To keep the repository lean and platform‑agnostic these native dependencies are downloaded on demand and stored in a runtime‑specific folder.

## Runtime directory

`RuntimeInfo` exposes the current runtime identifier (RID) like `win-x64`, `linux-x64` or `linux-arm64` and the path to `runtimes/<rid>/native` inside the application root. `INativeLibraryManager` searches this folder and returns `FileInfo` objects for libraries or executables:

```csharp
var manager = services.Resolve<INativeLibraryManager>();

var exe = manager.GetNativeExecutable("wkhtmltopdf");
```

If the file is missing or its version falls outside the requested range the manager deletes it so a fresh copy can be installed.

## Installing from NuGet packages

`INativeLibraryInstaller` downloads a NuGet package containing platform‑specific binaries and copies the correct file into the runtime directory. Packages follow the `runtimes/<rid>/native` layout.

```csharp
var manager = services.Resolve<INativeLibraryManager>();
var wkhtml = manager.GetNativeExecutable("wkhtmltopdf");

if (!wkhtml.Exists)
{
    using var installer = manager.CreateLibraryInstaller();
    wkhtml = await installer.InstallFromPackageAsync(
        new InstallNativePackageRequest("wkhtmltopdf", isExecutable: true, packageId: "Smartstore.wkhtmltopdf.Native")
        {
            MinVersion = "0.12.6"
        });
}
```

`InstallNativePackageRequest` can define minimum and maximum versions and optionally append the current RID to the package id.

## Packing native files

Native libraries are distributed as regular NuGet packages. Each RID contains its own `native` folder:

```
Smartstore.TinyImage.Png.Native.linux-arm64.nupkg
 └─ runtimes/
    └─ linux-arm64/native/pngquant
```

When the installer runs, the appropriate file is copied to the application directory together with a `.version` sidecar containing the native package version. Executables are made executable on Unix during deployment, because NuGet packages do not preserve Unix file modes. Subsequent calls to `GetNativeExecutable` or `GetNativeLibrary` then resolve immediately without additional downloads.
