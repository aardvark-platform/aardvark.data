# Aardvark.Data AI Reference

Index for AI coding assistants. **Read only the section you need.**

## Quick Reference

For one-liner APIs, see [QUICK_REF.md](QUICK_REF.md) (~100 lines).

## By Workflow

| Workflow | Document | Section | Key APIs |
|----------|----------|---------|----------|
| Load single 3D file | DATA_FORMATS.md | varies | `GLTF.load`, `ColladaImporter.Load`, `ObjParser.Load` |
| Load point cloud hierarchy | DATA_FORMATS.md | 496-651 | `PatchHierarchy.load`, `Aara.fromFile` |
| Process IFC building | DATA_FORMATS.md | 234-495 | `IFCParser.PreprocessIFC`, `PolyMesh`, `EndCaching` |
| Multi-format loader | QUICK_REF.md | 86-100 | Plugin pattern |
| Texture loading | PIXIMAGE_LOADERS.md | 22-65 | `PixImageMipMap.Load`, backend selection |
| Choose image loader | PIXIMAGE_LOADERS.md | 22-65 | Decision tree |

## By Task

| Task | Document | Lines | Keywords |
|------|----------|-------|----------|
| Image loading | [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md) | 1-526 | PNG, JPEG, TIFF, BMP, DDS, TGA, WebP, EXR, HDR |
| 3D formats | [DATA_FORMATS.md](DATA_FORMATS.md) | 1-1440 | COLLADA, glTF, OBJ, VRML, IFC, mesh, scene |
| Testing | [TESTING.md](TESTING.md) | 1-418 | NUnit, Expecto, FsCheck, benchmark |

## By Loader (with line ranges)

### Image Loaders ([PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md))

| Loader | Lines | Keywords |
|--------|-------|----------|
| Overview + comparison | 1-21 | which loader, comparison |
| Backend selection guide | 22-65 | decision tree |
| ImageSharp (recommended) | 66-130 | PNG, JPEG, TIFF, WebP, cross-platform |
| Pfim | 131-180 | DDS, TGA, mipmaps, game textures |
| FreeImage | 181-248 | EXR, HDR, RAW, 30+ formats, native |
| DevIL | 249-305 | legacy, native |
| SystemDrawing | 306-363 | Windows, GDI+, Bitmap |
| WindowsMedia | 364-422 | Windows, WIC, BitmapSource |
| Usage patterns | 423-506 | load, save, stream |
| Gotchas | 507-523 | platform, BGR, thread safety |

### 3D/Geometry Loaders ([DATA_FORMATS.md](DATA_FORMATS.md))

| Loader | Lines | Keywords |
|--------|-------|----------|
| Overview + comparison | 1-21 | which format, comparison |
| COLLADA | 22-95 | .dae, scene graph, materials, lights |
| glTF 2.0 | 96-233 | .gltf, .glb, PBR, modern |
| IFC | 234-495 | .ifc, building, BIM, Xbim, caching, edit after import, assemblies, snap positions, curves, PolyLineSet |
| OPC | 496-651 | point cloud, .aara, hierarchy |
| Wavefront OBJ | 652-759 | .obj, .mtl, simple mesh |
| VRML97 | 760-905 | .wrl, legacy, animation |
| DGM | 906-991 | heightfield, terrain, elevation |
| Photometry | 992-1113 | .ies, .ldt, light measurement |
| Usage patterns | 1114-1252 | traverse, flatten, convert |
| Gotchas | 1253-1428 | Xbim version, F# interop, coordinates |

### Testing ([TESTING.md](TESTING.md))

| Section | Lines | Keywords |
|---------|-------|----------|
| Running tests | 19-55 | dotnet test, filter |
| CSharpTests (NUnit) | 58-95 | C#, NUnit, TestFixture |
| FSharpTests (Expecto) | 96-135 | F#, Expecto, embedded resources |
| IfcTests | 136-174 | IFC, multi-target, round trip, edit after import |
| PixLoaderTests | 175-250 | property-based, FsCheck, benchmark |
| FreeImageTests | 251-272 | native, FreeImage.dll |
| Adding new tests | 273-358 | NUnit pattern, Expecto pattern, FsCheck |
| Embedded resources | 359-391 | test data, ManifestResourceStream |
| Gotchas | 392-413 | Windows-only, native DLLs, temp files |

## By Project

### Image Loaders (Aardvark.PixImage.*)
- `PixImageDevIL`, `PixImageFreeImage` → [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md)
- `PixImageImageSharp`, `PixImagePfim` → [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md)
- `PixImageSystemDrawing`, `PixImageWindowsMedia` → [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md)

### Data Format Loaders (Aardvark.Data.*)
- `ColladaLoader`, `GltfLoader` → [DATA_FORMATS.md](DATA_FORMATS.md)
- `IfcLoader`, `OpcLoader` → [DATA_FORMATS.md](DATA_FORMATS.md)
- `WavefrontLoader`, `Vrml97Loader` → [DATA_FORMATS.md](DATA_FORMATS.md)
- `DgmLoader`, `PhotometryLoader` → [DATA_FORMATS.md](DATA_FORMATS.md)

### Testing
- Test projects, frameworks, patterns → [TESTING.md](TESTING.md)

## Format Support Matrix

| Format | Project | Language | Cross-Platform |
|--------|---------|----------|----------------|
| PNG, JPEG, etc. | PixImage.ImageSharp | F# | Yes |
| DDS, TGA | PixImage.Pfim | F# | Yes |
| Various (native) | PixImage.FreeImage | C# | Yes (needs native) |
| Various (native) | PixImage.DevIL | C# | Yes (needs native) |
| GDI+ formats | PixImage.SystemDrawing | C# | Windows only |
| WIC formats | PixImage.WindowsMedia | C# | Windows only |
| COLLADA (.dae) | Data.Collada | C# | Yes |
| glTF 2.0 | Data.GLTF | F# | Yes |
| IFC | Data.Ifc | C# | Yes |
| OPC | Data.Opc | F# | Yes |
| Wavefront OBJ | Data.Wavefront | C# | Yes |
| VRML97 | Data.Vrml97 | C# | Yes |
