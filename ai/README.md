# Aardvark.Data AI Reference

Index for AI coding assistants. **Read only the section you need.**

## Quick Reference

For one-liner APIs, see [QUICK_REF.md](QUICK_REF.md) (~100 lines).

## By Workflow

| Workflow | Document | Section | Key APIs |
|----------|----------|---------|----------|
| Load single 3D file | DATA_FORMATS.md | varies | `GLTF.load`, `ColladaImporter.Load`, `ObjParser.Load` |
| Load point cloud hierarchy | DATA_FORMATS.md | 415-567 | `PatchHierarchy.load`, `Aara.fromFile` |
| Process IFC building | DATA_FORMATS.md | 233-411 | `IFCParser.PreprocessIFC`, `PolyMesh` |
| Multi-format loader | QUICK_REF.md | 79-92 | Plugin pattern |
| Texture loading | PIXIMAGE_LOADERS.md | 22-62 | `PixImageMipMap.Load`, backend selection |
| Choose image loader | PIXIMAGE_LOADERS.md | 22-62 | Decision tree |

## By Task

| Task | Document | Lines | Keywords |
|------|----------|-------|----------|
| Image loading | [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md) | 1-483 | PNG, JPEG, TIFF, BMP, DDS, TGA, WebP, EXR, HDR |
| 3D formats | [DATA_FORMATS.md](DATA_FORMATS.md) | 1-1212 | COLLADA, glTF, OBJ, VRML, IFC, mesh, scene |
| Testing | [TESTING.md](TESTING.md) | 1-416 | NUnit, Expecto, FsCheck, benchmark |

## By Loader (with line ranges)

### Image Loaders ([PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md))

| Loader | Lines | Keywords |
|--------|-------|----------|
| Overview + comparison | 1-20 | which loader, comparison |
| ImageSharp (recommended) | 22-84 | PNG, JPEG, TIFF, WebP, cross-platform |
| Pfim | 87-134 | DDS, TGA, mipmaps, game textures |
| FreeImage | 137-203 | EXR, HDR, RAW, 30+ formats, native |
| DevIL | 206-259 | legacy, native |
| SystemDrawing | 262-318 | Windows, GDI+, Bitmap |
| WindowsMedia | 320-377 | Windows, WIC, BitmapSource |
| Usage patterns | 379-440 | load, save, stream |
| Gotchas | 463-477 | platform, BGR, thread safety |

### 3D/Geometry Loaders ([DATA_FORMATS.md](DATA_FORMATS.md))

| Loader | Lines | Keywords |
|--------|-------|----------|
| Overview + comparison | 1-20 | which format, comparison |
| COLLADA | 22-94 | .dae, scene graph, materials, lights |
| glTF 2.0 | 96-231 | .gltf, .glb, PBR, modern |
| IFC | 233-343 | .ifc, building, BIM, Xbim |
| OPC | 345-438 | point cloud, .aara, hierarchy |
| Wavefront OBJ | 441-528 | .obj, .mtl, simple mesh |
| VRML97 | 530-674 | .wrl, legacy, animation |
| DGM | 676-760 | heightfield, terrain, elevation |
| Photometry | 762-882 | .ies, .ldt, light measurement |
| Usage patterns | 885-1020 | traverse, flatten, convert |
| Gotchas | 1024-1197 | Xbim version, F# interop, coordinates |

### Testing ([TESTING.md](TESTING.md))

| Section | Lines | Keywords |
|---------|-------|----------|
| Running tests | 19-54 | dotnet test, filter |
| CSharpTests (NUnit) | 56-92 | C#, NUnit, TestFixture |
| FSharpTests (Expecto) | 94-133 | F#, Expecto, embedded resources |
| IfcTests | 135-168 | IFC, multi-target |
| PixLoaderTests | 170-229 | property-based, FsCheck, benchmark |
| Adding new tests | 269-352 | NUnit pattern, Expecto pattern, FsCheck |
| Embedded resources | 354-386 | test data, ManifestResourceStream |

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
