# Aardvark.Data Quick Reference

One-liner APIs for common tasks. For details, see the full doc linked in each section.

## Image Loading ([PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md))

```fsharp
// Load (auto-detect loader)
let img = PixImage.Load("photo.jpg")

// Save with quality
img.SaveAsImage("out.jpg", PixJpegSaveParams(Quality = 90))
img.SaveAsImage("out.png", PixPngSaveParams(CompressionLevel = 6))

// Recommended loader: ImageSharp (cross-platform, pure C#)
let img = PixImageSharp.Create("photo.jpg")

// DDS/TGA with mipmaps: use Pfim
let mipmap = PixImagePfim.LoadWithMipmap("texture.dds")
```

**Loader choice**: ImageSharp (general), Pfim (DDS/TGA), FreeImage (30+ formats, needs native DLL)

## 3D Formats ([DATA_FORMATS.md](DATA_FORMATS.md))

```csharp
// COLLADA
List<ColladaNode> nodes = ColladaImporter.Load("scene.dae");

// glTF (F#)
let scene = GLTF.load "model.gltf"

// Wavefront OBJ
var obj = ObjParser.Load("model.obj");
var meshes = obj.ToPolyMeshes();

// IFC (building models) - always use singleThreading!
var ifc = IFCParser.PreprocessIFC("building.ifc", singleThreading: true);
foreach (var (guid, content) in ifc.Content)
{
    PolyMesh mesh = content.Geometry;
    Trafo3d trafo = content.Trafo;
}

// VRML97
var scene = SceneLoader.Load("scene.wrl");
```

## Point Clouds & Terrain ([DATA_FORMATS.md](DATA_FORMATS.md))

```fsharp
// OPC: Load hierarchy with serialization
let hierarchy = PatchHierarchy.load pickle unpickle (OpcPaths opcDir)
let patches = hierarchy.tree |> QTree.flatten |> Array.filter (fun p -> p.level = 2)

// OPC: Load patch data
let positions = Aara.fromFile<V3f> "patch/Positions.aara"
let normals = Aara.fromFile<V3f> "patch/Normals.aara"

// DGM heightfield
let geometry, dgm = DGM.dgm2IndexGeometry "terrain.dgm"
```

## Photometry ([DATA_FORMATS.md](DATA_FORMATS.md))

```csharp
var ies = IESParser.Parse("light.ies");    // IES format
var ldt = LDTParser.Parse("light.ldt");    // EULUMDAT format
```

## Testing ([TESTING.md](TESTING.md))

```bash
dotnet test src/Tests/CSharpTests           # NUnit (C#)
dotnet test src/Tests/FSharpTests           # Expecto (F#)
dotnet test --filter "Name~LoadPrimitive"   # Filter by name
```

## Multi-Format Loader Pattern

```csharp
// Plugin-style loader dispatcher
var loaders = new Dictionary<string, Func<string, List<PolyMesh>>> {
    { ".gltf", path => GLTF.load(path).Meshes.Values.ToList() },
    { ".glb",  path => GLTF.load(path).Meshes.Values.ToList() },
    { ".dae",  path => ColladaImporter.Load(path).SelectMany(GetMeshes).ToList() },
    { ".obj",  path => ObjParser.Load(path).ToPolyMeshes() },
    { ".wrl",  path => SceneLoader.Load(path).GetMeshes().ToList() },
};
var ext = Path.GetExtension(file).ToLower();
if (loaders.TryGetValue(ext, out var loader)) meshes = loader(file);
```

## Common Gotchas

| Issue | Fix |
|-------|-----|
| Native DLL missing | FreeImage/DevIL need native binaries in output |
| Windows-only loader | SystemDrawing, WindowsMedia fail on Linux/macOS |
| IFC threading deadlock | Use `singleThreading: true` |
| Coordinate system | COLLADA/glTF = Y-up; IFC = Z-up |
| OPC folder invalid | Check for `Patches/`, `Images/`, `PatchHierarchy.xml` |
| PolyMesh cleanup | Use `mesh.WithCompactedVertices` after loading |
