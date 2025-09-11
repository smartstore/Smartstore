---
description: Media library, storage providers and IMediaService usage
---

# Media

## Overview

Smartstore ships with a comprehensive media library for images, videos and downloadable files. Metadata about each file is stored in the `MediaFile` table while the binary data lives in a pluggable storage provider. Files are grouped in folders, tagged and tracked so that unused assets can be cleaned up safely.

`IMediaService` is the central entry point used by both the admin UI and application code. It hides where files are physically stored and exposes helpers for querying, saving and deleting media.

## Storage providers

The physical blob for a `MediaFile` is managed by an `IMediaStorageProvider`. Two providers are included:

* **FileSystemMediaStorageProvider** – stores files under `/Storage` on disk. This is the default provider used in new installations.
* **DatabaseMediaStorageProvider** – persists binary data in the database for scenarios where a shared file system is not available.

Providers implement `IMediaSender`/`IMediaReceiver` so media can be moved between backends. `MediaStorageConfiguration` determines which provider is active and the `MediaMover` service performs migrations.

You can specify which provider to use in the `MediaSettings`.

## Working with media

`IMediaService` offers high level operations for everyday tasks:

* Locate files via `GetFileByIdAsync`, `GetFileByPathAsync` or by running a `MediaSearchQuery`.
* Create and update files with `SaveFileAsync`.
* Copy, move or delete files, taking care of duplicate detection and track checks.
* Generate public URLs through `GetUrl`, which internally cooperates with the imaging subsystem for resizing and caching.

### Upload example

```csharp
public class UploadService
{
    private readonly IMediaService _mediaService;

    public UploadService(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public async Task<string> UploadAsync(Stream stream, string fileName)
    {
        // Store file permanently inside the "uploads" folder.
        var path = _mediaService.CombinePaths("uploads", fileName);
        var info = await _mediaService.SaveFileAsync(path, stream, isTransient: false);

        // Build a public URL. Additional ProcessImageQuery options can resize on the fly.
        return _mediaService.GetUrl(info, new ProcessImageQuery());
    }
}
```

Setting `ImagePostProcessingEnabled` to `false` temporarily disables expensive image transformations during bulk imports.

## Tracking and metadata

Each media file may have tags and localized ALT or title text. When a file is associated with other entities (e.g. a product picture) a `MediaTrack` record is created. Tracking prevents files from being deleted accidentally and allows the system to detect orphaned media.

Duplicate files can be detected by the `IMediaDupeDetectorFactory` before saving. Querying helpers like `CountFilesGroupedAsync` or `SearchFilesAsync` support housekeeping tasks in custom code.

## URLs and imaging

`IMediaUrlGenerator` creates public URLs under predictable paths such as `/media/123/thumbnail.jpg`. When a `ProcessImageQuery` is supplied to `GetUrl` the imaging pipeline resizes or crops the image and caches the result in `/media/thumbs` as documented in the [Imaging](imaging.md) section.

{% hint style="info" %}
Some administrative tasks require the Media-Manager plugin.
{% endhint %}
