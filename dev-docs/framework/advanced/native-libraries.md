# Native libraries

Some Smartstore features rely on OS‑specific binaries such as PDF engines or image processors. To keep the repository lean and platform‑agnostic these native dependencies are downloaded on demand and stored in a runtime‑specific folder.

## Runtime directory

`RuntimeInfo` exposes the current runtime identifier (RID) like `win-x64` or `linux-x64` and the path to `runtimes/<rid>/native` inside the application root. `INativeLibraryManager` searches this folder and returns `FileInfo` objects for libraries or executables:

```csharp
var manager = services.Resolve<INativeLibraryManager>();

var lib = manager.GetNativeLibrary("libsass", minVersion: "3.6");
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
Smartstore.wkhtmltopdf.Native.nupkg
 └─ runtimes/
    ├─ win-x64/native/wkhtmltopdf.exe
    ├─ linux-x64/native/wkhtmltopdf
    └─ osx-x64/native/wkhtmltopdf
```

When the installer runs, the appropriate file is copied to the application's `runtimes/<rid>/native` directory. Subsequent calls to `GetNativeExecutable` or `GetNativeLibrary` then resolve immediately without additional downloads.