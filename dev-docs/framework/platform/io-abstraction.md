# ✔ IO abstraction

## Overview

The `IFileProvider` interface in .NET Core already provides file system abstraction, but it has a very simple signature and is only intended for reading files. For this reason, Smartstore provides the [IFileSystem](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/IO/IFileSystem.cs) interface, along with [IFile](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/IO/IFile.cs) and [IDirectory](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/IO/IDirectory.cs). Their signatures are comprehensive and more aligned with the classes in `System.IO`, which you should be more familiar with.

`IFileSystem` also implements `IFileProvider` and both `IFile` and `IDirectory` implement `IFileInfo`. So think of it as a kind of extension to `IFileProvider`. All Smartstore IO interfaces have async methods that `IFileProvider` doesn’t provide. This is necessary, because `IFileSystem` can also represent cloud storage providers (such as Azure BLOB storage) and virtualize database-driven file storage.

The virtual file system uses the forward slash (`/`) as path delimiter and has no concept of volumes or drives. All paths are specified and returned as relative paths to the root of the virtual file system. Absolute paths that use a leading slash (`/`), dot (`.`) or parent traversal (`../`) are not supported.

The default implementation of `IFileProvider` is [LocalFileSystem](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/IO/LocalFileSystem/LocalFileSystem.cs). It looks for files using the local disk file system. The async methods delegate to the sync methods.

Here are some basic examples:

```csharp
// Get the current directory path
var currentDirPath = System.IO.Directory.GetCurrentDirectory();
var localFS = new LocalFileSystem(currentDirPath);

// Get the "MyTemplates" directory
var currentDir = localFS.GetDirectory("MyTemplates");

// Get a list of files in the "MyTemplates" directory
var files = currentDir.EnumerateFiles();

// Get the first file
var firstFile = files.FirstOrDefault();

// Read the first 20 bytes of the file
var inStream = firstFile.OpenRead();
var buffer = new byte[20];
inStream.Read(buffer);
inStream.Dispose();

// Convert buffer to string
string firstString = System.Text.Encoding.Default.GetString(buffer);

var dirListing = "Directory_Listing.txt";

// Check for unique file name
var isNotUnique = localFS.CheckUniqueFileName(dirListing, out var uniqueDirListing);

// Create new file instance
var file = localFS.CreateFile(isNotUnique ? uniqueDirListing : dirListing);

// Create some content
var numberOfFiles = currentDir.CountFiles(deep: false);
var fileNames = new string[files.Count()];

// List all files in current directory
for (var i = 0; i < numberOfFiles; i++)
{
    fileNames[i] = files.ElementAt(i).Name;
}

var content = $"The first File in this directory starts with:\n\"{firstString}\"\n\n"
    + $"The {numberOfFiles} files included are:\n\n" + fileNames.StrJoin("\n");

// Write contents to file
var outStream = file.OpenWrite();
outStream.Write(content.ToAsciiBytes());
outStream.Dispose();
```

## Adapters & decorators

### ExpandedFileSystem

The [ExpandedFileSystem](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/IO/ExpandedFileSystem.cs) is a decorator that takes a path prefix and an inner `IFileSystem` instance. Its root path is expanded with the specified prefix.

```csharp
var storeRoot = new LocalFileSystem(System.IO.Directory.GetCurrentDirectory());

// Count all the english email templates
var efs = new ExpandedFileSystem("/App_Data/EmailTemplates", storeRoot);
// "en" actually resolves to "/App_Data/EmailTemplates/en"
var numberOfFiles = efs.CountFiles("en");
```

### CompositeFileSystem

The [CompositeFileSystem](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/IO/CompositeFileSystem.cs) is an adapter that looks up files using a collection of `IFileSystem` instances. Because it implements the `IFileSystem` interface, it can be used in the same way.

```csharp
var storeRoot = new LocalFileSystem(System.IO.Directory.GetCurrentDirectory());

var controllerRoot = new LocalFileSystem(
    storeRoot.GetDirectory("Controllers").PhysicalPath
);
var viewRoot = new LocalFileSystem(
    storeRoot.GetDirectory("Views").PhysicalPath
);
var cfs = new CompositeFileSystem(controllerRoot, viewRoot);

var hasShoppingCartController = cfs.FileExists("ShoppingCartController.cs");
var hasShoppingCartViewDir = cfs.DirectoryExists("ShoppingCart");
```

## Special utilities

### DirectoryHasher

The [DirectoryHasher](https://github.com/smartstore/Smartstore/blob/main/src/Smartstore/IO/DirectoryHasher.cs) creates a hash of the contents of a directory. It can load and persist the hash code for comparison with any previous state, that are saved in **App\_Data/Tenants/Default/Hash**. By default, it scans flat, but you can choose to scan deep as well.

```csharp
var storeRoot = new LocalFileSystem(System.IO.Directory.GetCurrentDirectory());
var hasher = storeRoot.GetDirectoryHasher("Controllers", deep: true);
var hash = hasher.CurrentHash;
```

