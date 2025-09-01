---
description: Dynamic image processing, resizing and caching
---

# Imaging

## Overview
Smartstore provides an imaging pipeline backed by [SixLabors ImageSharp](https://docs.sixlabors.com/). Images can be loaded, transformed and re-encoded on the fly. Results are cached under `media/Thumbs` so repeated requests are served quickly.

## Processing images
`IImageProcessor` orchestrates resizing and encoding. Describe the operation with `ProcessImageQuery` and pass it to `ProcessImageAsync`:

```csharp
var query = new ProcessImageQuery("~/wwwroot/images/sample.jpg")
{
    MaxWidth = 300,
    MaxHeight = 200,
    Quality = 80,
    ScaleMode = "crop"
};

using var result = await _imageProcessor.ProcessImageAsync(query);
await result.Image.SaveAsync("~/wwwroot/output/thumb.jpg");
```

### Query tokens
`ProcessImageQuery` accepts these standard tokens:

| Token | Description |
| ----- | ----------- |
| `w`, `h` | width/height in pixels |
| `size` | apply same size to both dimensions |
| `q` | JPEG quality (0-100) |
| `m` | scale mode (`max`, `crop`, `pad`, `stretch`, `boxpad`, `min`) |
| `pos` | crop anchor (`top`, `bottom-right`, …) |

Tokens are validated so unsupported sizes or values are rejected before processing.

## Caching and URLs
The processor cooperates with `IImageCache` and `IMediaService` to produce predictable thumb paths like `/media/thumbs/0001/1234567-photo-w300-h200.jpg`. `MediaService.GetUrl` generates these URLs and the cache ensures that subsequent requests reuse the processed file.

## Extensibility
Two events wrap the pipeline:

- `ImageProcessingEvent` fires before transformations are applied, enabling custom effects.
- `ImageProcessedEvent` runs after processing to inspect or replace the result.

You can adjust defaults such as quality, resampling mode and cache location via `MediaSettings`.