# Aardvark.Data AI Reference

Index for AI coding assistants. **Read only the section you need.**

## Quick Reference

For one-liner APIs, see [QUICK_REF.md](QUICK_REF.md) (~100 lines).

## By Workflow

| Workflow | Document | Section | Key APIs |
|----------|----------|---------|----------|
| Load single 3D file | DATA_FORMATS.md | varies | `GLTF.load`, `ColladaImporter.Load`, `ObjParser.Load` |
| Load point cloud hierarchy | DATA_FORMATS.md | 417-571 | `PatchHierarchy.load`, `Aara.fromFile` |
| Process IFC building | DATA_FORMATS.md | 236-415 | `IFCParser.PreprocessIFC`, `PolyMesh` |
| Multi-format loader | QUICK_REF.md | 79-92 | Plugin pattern |
| Texture loading | PIXIMAGE_LOADERS.md | 22-62 | `PixImageMipMap.Load`, backend selection |
| Choose image loader | PIXIMAGE_LOADERS.md | 22-62 | Decision tree |

## By Task

| Task | Document | Lines | Keywords |
|------|----------|-------|----------|
| Image loading | [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md) | 1-483 | PNG, JPEG, TIFF, BMP, DDS, TGA, WebP, EXR, HDR |
| 3D formats | [DATA_FORMATS.md](DATA_FORMATS.md) | 1-1368 | COLLADA, glTF, OBJ, VRML, IFC, mesh, scene |
| Testing | [TESTING.md](TESTING.md) | 1-417 | NUnit, Expecto, FsCheck, benchmark |

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
| COLLADA | 22-96 | .dae, scene graph, materials, lights |
| glTF 2.0 | 98-234 | .gltf, .glb, PBR, modern |
| IFC | 236-415 | .ifc, building, BIM, Xbim |
| OPC | 417-571 | point cloud, .aara, hierarchy |
| Wavefront OBJ | 573-686 | .obj, .mtl, simple mesh |
| VRML97 | 688-832 | .wrl, legacy, animation |
| DGM | 834-918 | heightfield, terrain, elevation |
| Photometry | 920-1040 | .ies, .ldt, light measurement |
| Usage patterns | 1042-1179 | traverse, flatten, convert |
| Gotchas | 1181-1355 | Xbim version, F# interop, coordinates |

### Testing ([TESTING.md](TESTING.md))

| Section | Lines | Keywords |
|---------|-------|----------|
| Running tests | 19-54 | dotnet test, filter |
| CSharpTests (NUnit) | 58-96 | C#, NUnit, TestFixture, Wavefront, Photometry |
| FSharpTests (Expecto) | 98-136 | F#, Expecto, embedded resources |
| IfcTests | 138-172 | IFC, multi-target |
| PixLoaderTests | 174-248 | property-based, FsCheck, benchmark |
| FreeImageTests | 250-270 | FreeImage, NUnit, BenchmarkDotNet |
| Adding new tests | 272-356 | NUnit pattern, Expecto pattern, FsCheck |
| Embedded resources | 358-388 | test data, ManifestResourceStream |

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
