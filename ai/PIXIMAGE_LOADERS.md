# Aardvark.Data PixImage Loaders Reference

AI-targeted reference for image loading in Aardvark.

---

## Overview

| Loader | Backend | Platform | Best For |
|--------|---------|----------|----------|
| ImageSharp | Pure C# | All | General use (recommended) |
| Pfim | Pure C# | All | DDS, TGA (game textures, mipmaps) |
| FreeImage | Native | All (needs DLL) | Wide format support |
| DevIL | Native | All (needs DLL) | Legacy compatibility |
| SystemDrawing | GDI+ | Windows | Windows integration |
| WindowsMedia | WIC | Windows | Windows-specific formats |

**Loader registration**: Loaders auto-register via `[OnAardvarkInit]`. Last registered wins for format conflicts.

---

## Backend Selection Guide

**Decision Tree:**
```
Need to load images?
├── DDS or TGA files? → Use Pfim (mipmap support)
├── EXR, HDR, RAW, or exotic formats? → Use FreeImage (needs native DLL)
├── Windows-only app needing GDI+ interop? → Use SystemDrawing
├── Windows-only app needing WIC/BitmapSource? → Use WindowsMedia
└── Everything else → Use ImageSharp (recommended default)
```

**Initialization Patterns:**
```fsharp
// ImageSharp: auto-registers, but can force init
PixImageSharp.Init()  // Force initialization if needed

// Multiple loaders: last registered wins for overlapping formats
PixImage.AddLoader(PixImageSharp.Loader)   // General formats
PixImage.AddLoader(PixImagePfim.Loader)    // DDS/TGA override
```

**Texture Loading for 3D (PRo3D.SPICE pattern):**
```fsharp
// Load with automatic mipmap generation
let texture = PixTexture2d(PixImageMipMap.Load("diffuse.jpg"))

// Programmatic texture creation
let white = PixImage<byte>(Col.Format.RGBA, V2i.II)
white.GetMatrix<C4b>().SetByCoord(fun _ -> C4b.White)
let fallback = PixTexture2d(PixImageMipMap(white))
```

**Format-Specific Recommendations:**
| Use Case | Loader | Reason |
|----------|--------|--------|
| Web images (PNG, JPEG, WebP) | ImageSharp | Pure C#, EXIF auto-orient |
| Game textures (DDS) | Pfim | Mipmap support, BC compression |
| HDR/EXR lighting | FreeImage | Float formats, wide support |
| Legacy Windows apps | SystemDrawing | Bitmap interop |
| WPF/UWP apps | WindowsMedia | BitmapSource conversion |

---

## ImageSharp (Recommended)

**Package**: `Aardvark.PixImage.ImageSharp`
**Backend**: [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)
**Platform**: Cross-platform (pure C#)

### Registration

Auto-registered on initialization. Manual registration:

```fsharp
PixImage.AddLoader(PixImageSharp.Loader)
```

### Supported Formats

| Format | Load | Save | Notes |
|--------|------|------|-------|
| JPEG | ✓ | ✓ | Quality control |
| PNG | ✓ | ✓ | Compression levels |
| BMP | ✓ | ✓ | |
| GIF | ✓ | ✓ | |
| TIFF | ✓ | ✓ | Multiple compression modes |
| TGA | ✓ | ✓ | |
| WebP | ✓ | ✓ | Lossless/lossy |
| PBM | ✓ | ✓ | |

**Pixel formats**: Byte (8-bit), UShort (16-bit), Float (32-bit) per channel. Supports Gray, RGB, RGBA, BGR, BGRA.

### Usage

```fsharp
// Load image
let img = PixImageSharp.Create("photo.jpg")

// Load with auto-orientation from EXIF
let img = PixImageSharp.Create(stream)

// Save with quality
img.SaveImageSharp("output.jpg", 85)

// Save with parameters
img.SaveImageSharp("output.png", PixPngSaveParams(CompressionLevel = 6))

// Get info without loading pixels
let info = PixImageSharp.GetPixImageInfo("large.jpg")

// Get EXIF camera info
match PixImageSharp.TryGetCameraInfo("photo.jpg") with
| Some cam -> printfn "Camera: %s %s, Focal: %.1fmm" cam.make cam.model cam.focal
| None -> ()

// Resize
let resized = img.ResizedImageSharp(V2i(800, 600), ImageInterpolation.Lanczos)
```

### Special Features

- **EXIF auto-orientation**: Automatically rotates images based on EXIF orientation tag
- **Camera info extraction**: `TryGetCameraInfo()` reads EXIF metadata
- **Thumbnail extraction**: `TryGetThumb()` gets embedded EXIF thumbnail
- **High-quality resampling**: Multiple interpolation modes (Lanczos, Cubic, etc.)

---

## Pfim (DDS/TGA Specialist)

**Package**: `Aardvark.PixImage.Pfim`
**Backend**: [Pfim](https://github.com/nickbabcock/Pfim)
**Platform**: Cross-platform (pure C#)

### Registration

Auto-registered on initialization. Manual registration:

```fsharp
PixImage.AddLoader(PixImagePfim.Loader)
```

### Supported Formats

| Format | Load | Save | Notes |
|--------|------|------|-------|
| DDS | ✓ | ✗ | DirectDraw Surface (mipmaps) |
| TGA | ✓ | ✗ | Targa (various bit depths) |

**Special**: Supports mipmap loading from DDS files.

**Pixel formats**: RGB8, RGB24, RGBA32, RGBA16, R5G5B5, R5G6B5, R5G5B5A1

### Usage

```fsharp
// Load single level
let img = PixImagePfim.Load("texture.dds")

// Load with all mipmaps
let mipmap = PixImagePfim.LoadWithMipmap("texture.dds")
for i in 0 .. mipmap.LevelCount - 1 do
    let level = mipmap.[i]
    printfn "Level %d: %dx%d" i level.Size.X level.Size.Y

// Stream loading
use stream = File.OpenRead("texture.tga")
let img = PixImagePfim.Load(stream)
```

### Gotchas

1. **Read-only**: No save support
2. **Format limitations**: Only DDS and TGA
3. **Mipmap interface**: Use `PixImageMipMap` for multi-level textures

---

## FreeImage

**Package**: `Aardvark.PixImage.FreeImage`
**Backend**: FreeImage native library
**Platform**: All (requires native DLL deployment)

### Registration

Auto-registered on initialization:

```csharp
PixImage.AddLoader(PixImageFreeImage.Loader);
```

### Supported Formats

Extensive format support (30+ formats):

| Format | Load | Save | Notes |
|--------|------|------|-------|
| JPEG, J2K, JP2 | ✓ | ✓ | Quality control |
| PNG | ✓ | ✓ | Compression levels |
| TIFF | ✓ | ✓ | Multiple compression modes |
| BMP, ICO, GIF | ✓ | ✓ | |
| EXR | ✓ | ✓ | HDR, multiple compressions |
| HDR, PFM | ✓ | ✓ | Floating-point formats |
| DDS | ✓ | ✗ | |
| PSD, XPM, SGI | ✓ | ✗ | |
| WebP | ✓ | ✓ | Lossless/lossy |
| RAW | ✓ | ✗ | Camera RAW |
| and more... | | | |

**Pixel formats**: Byte, Short, UShort, Int, UInt, Float, Double. Supports BW, Gray, RGB, RGBA, BGR, BGRA variants.

### Usage

```csharp
// Load (automatic via PixImage)
var img = PixImage.Load("photo.exr");

// Save with JPEG quality
img.SaveToFile("output.jpg", new PixJpegSaveParams(90));

// Save EXR with compression
img.SaveToFile("output.exr", new PixExrSaveParams {
    Compression = PixExrCompression.Zip
});

// Save TIFF with LZW compression
img.SaveToFile("output.tif", new PixTiffSaveParams {
    Compression = PixTiffCompression.Lzw
});

// WebP lossless
img.SaveToFile("output.webp", new PixWebpSaveParams {
    Lossless = true
});
```

### Gotchas

1. **Native DLL required**: Must deploy FreeImage DLL with application
2. **BGR layout**: Internally uses BGR; automatic conversion handled
3. **WebP crashes**: WebP plugin crashes if pixel format unsupported (byte only)
4. **Format removal**: Removes alpha channel for formats that don't support it (JPEG)

---

## DevIL

**Package**: `Aardvark.PixImage.DevIL`
**Backend**: [DevIL](https://openil.sourceforge.net/) (OpenIL)
**Platform**: All (requires native DLL deployment)

### Registration

Auto-registered on initialization:

```csharp
PixImage.AddLoader(PixImageDevil.Loader);
```

### Supported Formats

| Format | Load | Save | Notes |
|--------|------|------|-------|
| BMP, ICO | ✓ | ✓ | |
| JPEG, JNG | ✓ | ✓ | Quality control |
| PNG | ✓ | ✓ | |
| TIFF | ✓ | ✓ (not stream) | Stream save unsupported |
| DDS | ✓ | ✓ | |
| GIF, MNG | ✓ | ✓ | |
| EXR | ✓ | ✓ | |
| HDR | ✓ | ✓ | |
| TGA, PCX, SGI | ✓ | ✓ | |
| PSD, RAW | ✓ | ✓ | |
| and more... | | | |

**Pixel formats**: Byte, Short, UShort, Int, UInt, Float, Double. Supports Gray, GrayAlpha, RGB, RGBA, BGR, BGRA.

### Usage

```csharp
// Load (automatic)
var img = PixImage.Load("texture.dds");

// Save with JPEG quality
img.SaveToFile("output.jpg", new PixJpegSaveParams(85));

// Save PNG (compression level ignored)
img.SaveToFile("output.png", new PixPngSaveParams());

// Get image info
var info = PixImage.GetInfo("large.jpg");
```

### Gotchas

1. **Native DLL required**: Must deploy DevIL DLLs
2. **TIFF stream limitation**: Cannot save TIFF to streams, only files
3. **PNG compression**: Does not support compression level setting
4. **Thread safety**: Uses global lock (`s_devilLock`)

---

## SystemDrawing (Windows Only)

**Package**: `Aardvark.PixImage.SystemDrawing`
**Backend**: System.Drawing (GDI+)
**Platform**: Windows only

### Registration

**Note**: SystemDrawing does NOT auto-register. Manual registration only:

```csharp
// Not auto-registered - use explicit methods
```

### Supported Formats

| Format | Load | Save | Notes |
|--------|------|------|-------|
| BMP | ✓ | ✓ | |
| GIF | ✓ | ✓ | |
| JPEG | ✓ | ✓ | Quality control |
| PNG | ✓ | ✓ | |
| TIFF | ✓ | ✓ | |
| WMF | ✓ | ✓ | Windows Metafile |

**Pixel formats**: ByteRGB, ByteBGR, ByteBGRA, ByteBW, ByteRGBP, UShortGray, UShortBGR, UShortBGRA, UShortBGRP

### Usage

```csharp
// Convert from System.Drawing.Bitmap
var bitmap = new Bitmap("photo.jpg");
var pixImage = bitmap.ToPixImage();

// Convert to System.Drawing.Bitmap
var bitmap = pixImage.ToSystemDrawingBitmap();

// Save via SystemDrawing
pixImage.SaveViaSystemDrawing("output.jpg", new PixJpegSaveParams(90));
pixImage.SaveViaSystemDrawing("output.png", PixFileFormat.Png);

// Save as JPEG with quality
pixImage.SaveViaSystemDrawingAsJpeg("output.jpg", 85);

// Stream operations
using var stream = File.OpenWrite("output.png");
pixImage.SaveViaSystemDrawing(stream, PixFileFormat.Png);
```

### Gotchas

1. **Windows only**: Fails on Linux/macOS
2. **Not a loader**: Does not register with PixImage loader system
3. **Extension methods only**: Use explicit `SaveViaSystemDrawing()` methods
4. **Dense layout required**: Automatically converts to dense layout

---

## WindowsMedia (Windows Only)

**Package**: `Aardvark.PixImage.WindowsMedia`
**Backend**: Windows Imaging Component (WIC)
**Platform**: Windows only

### Registration

Auto-registered on initialization:

```csharp
PixImage.AddLoader(PixImageWindowsMedia.Loader);
```

### Supported Formats

| Format | Load | Save | Notes |
|--------|------|------|-------|
| PNG | ✓ | ✓ | |
| BMP | ✓ | ✓ | |
| JPEG | ✓ | ✓ | Quality control |
| TIFF | ✓ | ✓ | Compression modes |
| GIF | ✓ | ✓ | |
| WMP | ✓ | ✓ | Windows Media Photo |

**Pixel formats**: Byte, UShort, Float. Supports BW, Gray, RGB, BGR, RGBA, BGRA, RGBP, BGRP.

### Usage

```csharp
// Load (automatic via PixImage)
var img = PixImage.Load("photo.wmp");

// Save JPEG with quality
img.SaveToFile("output.jpg", new PixJpegSaveParams(90));

// Save TIFF with compression
img.SaveToFile("output.tif", new PixTiffSaveParams {
    Compression = PixTiffCompression.Lzw
});

// Convert to WPF BitmapSource
var bitmapSource = img.ToBitmapSource(dpi: 96.0);

// Convert from System.Drawing.Bitmap via WIC
var pixImage = bitmap.ToPixImageViaWindowsMedia();

// Get image info
var info = PixImage.GetInfo("large.jpg");
```

### Gotchas

1. **Windows only**: Fails on Linux/macOS
2. **PNG compression**: Does not support compression level setting
3. **Indexed formats**: Automatically converts to BGRA32

---

## Usage Patterns

### Loading an Image

```fsharp
// Automatic loader selection (uses registered loaders)
let img = PixImage.Load("photo.jpg")

// Force specific loader
let img = PixImageSharp.Create("photo.jpg")

// Load from stream
use stream = File.OpenRead("image.png")
let img = PixImage.Load(stream)

// Check if file is loadable
match PixImageSharp.TryCreate("maybe.jpg") with
| Some img -> printfn "Loaded: %dx%d" img.Size.X img.Size.Y
| None -> printfn "Not an image"

// Get info without loading pixels
let info = PixImage.GetInfo("huge.tif")
printfn "Size: %A, Format: %A" info.Size info.PixFormat
```

### Saving an Image

```fsharp
// Auto-detect format from extension
img.SaveAsImage("output.png")

// Explicit format
img.SaveAsImage("output", PixFileFormat.Jpeg)

// With quality/compression
img.SaveAsImage("output.jpg", PixJpegSaveParams(Quality = 90))
img.SaveAsImage("output.png", PixPngSaveParams(CompressionLevel = 6))
img.SaveAsImage("output.tif", PixTiffSaveParams(Compression = PixTiffCompression.Lzw))

// Force specific loader
img.SaveImageSharp("output.webp", PixWebpSaveParams(Lossless = true))

// To stream
use stream = File.Create("output.jpg")
img.SaveAsImage(stream, PixJpegSaveParams(Quality = 85))

// To memory
let data = img |> PixImage.ToByteArray PixFileFormat.Png
```

### Format Detection

```fsharp
// Get loader info
let loaders = PixImage.GetLoaders()
for loader in loaders do
    printfn "%s: encode=%b decode=%b" loader.Name loader.CanEncode loader.CanDecode

// Check format support
let format = PixFileFormat.FromExtension(".webp")
```

### Cross-Platform Pattern

```fsharp
// Recommended: Use ImageSharp for cross-platform
#if WINDOWS
open Aardvark.Data.PixImageWindowsMedia
#endif

// Platform-specific optimization
let loader =
    if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
        // Use WIC on Windows for better integration
        PixImageWindowsMedia.Loader
    else
        // Use ImageSharp everywhere else
        PixImageSharp.Loader

PixImage.AddLoader(loader)
```

---

## Gotchas

1. **Platform-specific loaders**: SystemDrawing and WindowsMedia fail on Linux/macOS
2. **Native DLL deployment**: FreeImage and DevIL require native binaries in output directory
3. **Registration order**: Last registered loader wins for format conflicts
4. **Memory management**: Large images may need explicit disposal (streams, native resources)
5. **Format auto-conversion**: Some loaders convert formats automatically (e.g., indexed → RGBA)
6. **Alpha channel handling**: Formats without alpha support (JPEG) silently drop alpha channel
7. **BGR vs RGB**: Native loaders (FreeImage, WindowsMedia) often use BGR internally; conversion is automatic
8. **Thread safety**: DevIL uses global lock; ImageSharp is thread-safe
9. **Stream position**: Some loaders may not reset stream position; rewind manually if reusing
10. **EXIF orientation**: Only ImageSharp auto-orients by default; others need manual handling
11. **Mipmap support**: Only Pfim supports mipmap loading; use `LoadWithMipmap()` for DDS
12. **Save parameters**: Not all loaders support all save parameters (check implementation)

---

## See Also

- [DATA_FORMATS.md](DATA_FORMATS.md) - 3D format loaders
