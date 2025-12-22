# Aardvark.Data Format Loaders Reference

AI-targeted reference for 3D and geometry format loading in Aardvark.Data.

---

## Overview

| Format | Project | Language | Output Type | Primary Use |
|--------|---------|----------|-------------|-------------|
| COLLADA (.dae) | Data.Collada | C# | Scene graph (ColladaNode list) | 3D scenes with materials/lights |
| glTF 2.0 (.gltf, .glb) | Data.GLTF | F# | Scene (PBR materials) | Modern 3D assets |
| IFC (.ifc) | Data.Ifc | C# | Building geometry (IFCData) | Building information models |
| OPC | Data.Opc | F# | Point cloud hierarchy | Large-scale point clouds |
| Wavefront (.obj) | Data.Wavefront | C# | WavefrontObject | Simple mesh import |
| VRML97 (.wrl) | Data.Vrml97 | C# | VrmlScene | Legacy 3D scenes |
| DGM | Data.DGM | F# | IndexedGeometry | Digital elevation models |
| Photometry (.ies, .ldt) | Data.Photometry | C# | IESData / LDTData | Light measurements |

---

## COLLADA Loader

### Namespace
```csharp
using Aardvark.Data.Collada;
```

### API

**Load scene from file:**
```csharp
List<ColladaNode> ColladaImporter.Load(string path)
List<ColladaNode> ColladaImporter.Load(Stream stream)
```

**Extract specific components:**
```csharp
Dictionary<string, List<PolyMesh>> ColladaImporter.GetGeometries(COLLADA collada)
Dictionary<string, ColladaMaterial> ColladaImporter.GetMaterials(COLLADA collada)
Dictionary<string, ColladaLight> ColladaImporter.GetLights(COLLADA collada)
List<ColladaNode> ColladaImporter.GetSceneTree(COLLADA collada)
```

### Key Types
- `ColladaNode` - Scene graph node with optional `Trafo3d`, children, geometry
- `ColladaGeometryNode` - Leaf node containing `PolyMesh` + `ColladaMaterial`
- `ColladaMaterial` - Material with ambient/diffuse/specular colors and texture paths
- `ColladaLight` - Base class: `ColladaPointLight`, `ColladaSpotLight`, `ColladaDirectionalLight`, `ColladaAmbientLight`

### Usage
```csharp
// Load complete scene hierarchy
var sceneNodes = ColladaImporter.Load(@"C:\models\scene.dae");

// Traverse scene graph
void ProcessNode(ColladaNode node, Trafo3d parentTrafo)
{
    var localTrafo = node.Trafo ?? Trafo3d.Identity;
    var worldTrafo = parentTrafo * localTrafo;

    foreach (var child in node)
    {
        if (child is ColladaGeometryNode geomNode)
        {
            var mesh = geomNode.Mesh;
            var material = geomNode.Material;
            // Process geometry with worldTrafo
        }
        else if (child is ColladaNode groupNode)
        {
            ProcessNode(groupNode, worldTrafo);
        }
    }
}

foreach (var root in sceneNodes)
    ProcessNode(root, Trafo3d.Identity);
```

### Supported Features
| Feature | Supported | Notes |
|---------|-----------|-------|
| Triangles | ✓ | Primary geometry type |
| Polylist | ✓ | Converted to PolyMesh |
| Polygons | ✓ | Arbitrary polygons |
| Lines | ✗ | Warning emitted |
| Materials | ✓ | Phong/Lambert/Blinn/Constant |
| Textures | ✓ | Diffuse, specular, normal, emission |
| Lights | ✓ | Point, spot, directional, ambient |
| Skinning | ✓ | Via instance_controller |
| Multiple texcoords | ✓ | Indexed as TEXCOORD0, TEXCOORD1, etc. |

---

## glTF 2.0 Loader

### Namespace
```fsharp
open Aardvark.Data.GLTF
```

### API

**Load from file:**
```fsharp
Scene GLTF.load (file : string)
Scene option GLTF.tryLoad (file : string)
```

**Load from stream/data:**
```fsharp
Scene GLTF.readFrom (input : Stream)
Scene GLTF.ofArray (data : byte[])
Scene GLTF.ofString (data : string)
Scene GLTF.ofZipArchive (arch : ZipArchive)
```

**Save:**
```fsharp
unit GLTF.save (file : string) (scene : Scene)
byte[] GLTF.toArray (scene : Scene)
string GLTF.toString (scene : Scene)
```

### Key Types

**Scene:**
```fsharp
type Scene = {
    Materials   : Map<MaterialId, Material>
    Meshes      : Map<MeshId, Mesh>
    ImageData   : Map<ImageId, ImageData>
    RootNode    : Node
}
```

**Node:**
```fsharp
type Node = {
    Name            : option<string>
    Trafo           : option<Trafo3d>
    Meshes          : list<MeshInstance>
    Children        : list<Node>
}
```

**Mesh:**
```fsharp
type Mesh = {
    Name            : option<string>
    BoundingBox     : Box3d
    Mode            : IndexedGeometryMode
    Index           : option<int[]>
    Positions       : V3f[]
    Normals         : option<V3f[]>
    Tangents        : option<V4f[]>
    TexCoords       : list<V2f[] * Set<TextureSemantic>>
    Colors          : option<C4b[]>
}
```

**Material (PBR):**
```fsharp
type Material = {
    BaseColor                       : C4f
    BaseColorTexture                : option<ImageId>
    Roughness                       : float
    RoughnessTexture                : option<ImageId>
    Metallicness                    : float
    MetallicnessTexture             : option<ImageId>
    NormalTexture                   : option<ImageId>
    EmissiveColor                   : C4f
    EmissiveTexture                 : option<ImageId>
    DoubleSided                     : bool
    Opaque                          : bool
    // ...
}
```

### Usage
```fsharp
// Load glTF file
let scene = GLTF.load @"C:\models\model.gltf"

// Access materials
scene.Materials
|> Map.iter (fun matId material ->
    printfn "Material: %A" material.Name
    printfn "  BaseColor: %A" material.BaseColor
    printfn "  Roughness: %f" material.Roughness
)

// Traverse scene graph
let rec processNode (node : Node) =
    match node.Trafo with
    | Some t -> printfn "Transform: %A" t
    | None -> ()

    // Process meshes
    for meshInst in node.Meshes do
        match Map.tryFind meshInst.Mesh scene.Meshes with
        | Some mesh ->
            printfn "Mesh: %d vertices, mode=%A" mesh.Positions.Length mesh.Mode
            match meshInst.Material with
            | Some matId ->
                match Map.tryFind matId scene.Materials with
                | Some mat -> printfn "  Material: %A" mat.Name
                | None -> ()
            | None -> ()
        | None -> ()

    // Recurse children
    node.Children |> List.iter processNode

processNode scene.RootNode
```

### Supported Features
| Feature | Supported | Notes |
|---------|-----------|-------|
| Geometry modes | Triangles, Lines, Points, Strips | All IndexedGeometryMode |
| PBR materials | ✓ | Metallic-roughness workflow |
| Textures | ✓ | BaseColor, normal, roughness, metallic, emissive |
| Multiple UV sets | ✓ | Per-texture coordinate index |
| Vertex colors | ✓ | C4b format |
| Tangents | ✓ | V4f format |
| Animations | ✗ | Not implemented |
| Skinning | ✗ | Not implemented |
| Morph targets | ✗ | Not implemented |

---

## IFC Loader

### Namespace
```csharp
using Aardvark.Data.Ifc;
using Xbim.Ifc;
```

### Dependencies
Requires Xbim.Essentials NuGet packages. Version compatibility is critical.

### API

**Preprocess IFC file (includes geometry generation):**
```csharp
IFCData IFCParser.PreprocessIFC(
    string filePath,
    XbimEditorCredentials editor = null,
    XGeometryEngineVersion geometryEngine = XGeometryEngineVersion.V6,
    bool singleThreading = true)
```

### Key Types

**IFCData:**
```csharp
class IFCData
{
    IfcStore Model { get; }
    Dict<IfcGloballyUniqueId, IFCContent> Content { get; }
    Dictionary<string, IFCMaterial> Materials { get; }
    double ProjectScale { get; }
    // Caching handles, hierarchy, etc.
}
```

**IFCContent:**
```csharp
class IFCContent
{
    IIfcObject IfcObject { get; }
    PolyMesh Geometry { get; }
    Trafo3d Trafo { get; }
    IFCMaterial Material { get; }
    string RepresentationType { get; }
}
```

**IFCMaterial:**
```csharp
class IFCMaterial
{
    string Name { get; }
    XbimTexture Texture { get; }
    double? ThermalConductivity { get; }
    double? SpecificHeatCapacity { get; }
    double? MassDensity { get; }
}
```

### Usage
```csharp
// Preprocess IFC (generates geometry, may take time)
var ifcData = IFCParser.PreprocessIFC(@"C:\buildings\model.ifc", singleThreading: true);

// Access scale
double scale = ifcData.ProjectScale; // Typically 1e-3 for mm units

// Iterate geometry
foreach (var kvp in ifcData.Content)
{
    var guid = kvp.Key;
    var content = kvp.Value;

    var ifcObject = content.IfcObject;
    var name = ifcObject.Name?.ToString() ?? "Unnamed";
    var type = ifcObject.GetType().Name;

    var mesh = content.Geometry;
    var trafo = content.Trafo;
    var material = content.Material;

    Report.Line($"{type}: {name} - {mesh.VertexCount} vertices");
}

// Dispose properly
ifcData.Dispose();
```

### Geometry Structure
IFC loader returns `PolyMesh` per building element:
- `PositionArray` - V3d vertices (in IFC coordinate system)
- `VertexIndexArray` - Triangle indices (every 3 = 1 triangle)
- `FirstIndexArray` - Face boundaries (always triangles: [0, 3, 6, ...])
- `FaceVertexAttributes[Normals]` - Normal indices
- `FaceVertexAttributes[-Normals]` - Normal vectors (V3f)

### Supported Features
| Feature | Supported | Notes |
|---------|-----------|-------|
| IFC2x3 | ✓ | Via Xbim |
| IFC4 | ✓ | Via Xbim |
| IFC4x3 | ✓ | Via Xbim |
| Geometry tessellation | ✓ | Xbim geometry engine required |
| Materials | ✓ | Surface styles + associated materials |
| Hierarchical structure | ✓ | Via HierarchyExt |
| Spaces | Configurable | Can exclude via DefaultExclusions |
| Feature elements | ✗ | Excluded by default (openings, etc.) |

### IFC Processing Pipeline

**Mesh Extraction and Cleanup:**
```csharp
var ifc = IFCParser.PreprocessIFC(path, singleThreading: true);

foreach (var (guid, content) in ifc.Content)
{
    PolyMesh mesh = content.Geometry;

    // Clean up mesh (remove degenerate triangles, merge vertices)
    mesh = mesh.WithCompactedVertices;

    // Optional: cluster nearby vertices
    // mesh = mesh.WithClusteredVertices(1e-3);

    // Access material
    var material = content.Material;
    var color = material?.Texture?.Color ?? C4f.White;
}
```

**Geometry Engine Selection:**
```csharp
// V6 is recommended for IFC4+ files
var ifc = IFCParser.PreprocessIFC(path,
    geometryEngine: XGeometryEngineVersion.V6,  // or V5 for older files
    singleThreading: true);  // CRITICAL: avoid deadlocks
```

**Large Model Strategies:**
```csharp
// Process in batches to manage memory
var ifc = IFCParser.PreprocessIFC(path, singleThreading: true);

// Group by building storey for spatial organization
var byStorey = ifc.Content
    .GroupBy(kvp => GetStoreyName(kvp.Value.IfcObject))
    .ToDictionary(g => g.Key, g => g.ToList());

// Process one storey at a time, dispose early
foreach (var (storey, elements) in byStorey)
{
    ProcessStorey(storey, elements);
    // Allow GC between storeys
}

ifc.Dispose();  // Always dispose when done
```

**Material Mapping:**
```csharp
// IFCMaterial properties available
class IFCMaterial
{
    string Name;
    XbimTexture Texture;           // Contains Color (C4f)
    double? ThermalConductivity;   // Physical properties
    double? SpecificHeatCapacity;
    double? MassDensity;
}

// Convert to rendering material
var renderMaterial = new PbrMaterial {
    BaseColor = ifcMaterial.Texture?.Color ?? C4f.Gray,
    Roughness = 0.8f,  // IFC doesn't provide PBR params
    Metallic = 0.0f
};
```

---

## OPC Loader

### Namespace
```fsharp
open Aardvark.Data.Opc
```

### API

**Load patch hierarchy:**
```fsharp
PatchHierarchy PatchHierarchy.load (pickle : QTree<Patch> -> byte[]) (unpickle : byte[] -> QTree<Patch>) (opcPaths : OpcPaths)
```

**Load point data from .aara files:**
```fsharp
Volume<'T> Aara.fromFile<'T when 'T : unmanaged> (path : string)
Triangle3d[] Aara.loadTrianglesFromFile (aaraFile : string) (matrix : M44d)
```

### Key Types

**PatchHierarchy:**
```fsharp
type PatchHierarchy = {
    opcPaths : OpcPaths
    tree     : QTree<Patch>
}
```

**Patch:**
```fsharp
type Patch = {
    info         : PatchFileInfo
    level        : int
    triangleSize : float
    // ...
}
```

**Volume<'T>:**
```fsharp
type Volume<'T> = {
    Data : 'T[]
    Size : V3i
}
```

### Usage
```fsharp
open Aardvark.Data.Opc

// OPC paths structure
let opcPaths = OpcPaths.FromBaseDirectory @"C:\pointclouds\myopc"

// Load hierarchy with caching
let pickle qTree = ... // Serialization function
let unpickle bytes = ... // Deserialization function
let hierarchy = PatchHierarchy.load pickle unpickle opcPaths

// Access patch tree
let rootPatch = hierarchy.tree |> QTree.getRoot
printfn "Root patch: %s" rootPatch.info.Name

// Load point positions from .aara file
let positions = Aara.fromFile<V3f> @"C:\pointclouds\patch\Positions.aara"
printfn "Loaded %d points" positions.Data.Length
printfn "Grid size: %A" positions.Size

// Load as triangulated mesh
let triangles = Aara.loadTrianglesFromFile @"patch\Positions.aara" M44d.Identity
printfn "Generated %d triangles" triangles.Length
```

### File Structure
OPC format stores hierarchical point cloud patches:
- `PatchHierarchy.xml` - Hierarchy definition
- `patches/<name>/` - Per-patch directories
- `Positions.aara` - Point positions (binary)
- `Normals.aara` - Point normals (optional)
- `Colors.aara` - Point colors (optional)
- `.aara` format: Type name (string), dimensions (int), sizes (int[]), raw data

### Supported Features
| Feature | Supported | Notes |
|---------|-----------|-------|
| Hierarchical LOD | ✓ | QTree structure |
| Point positions | ✓ | V3f or V3d |
| Point normals | ✓ | Per-point normals |
| Point colors | ✓ | Per-point colors |
| Triangulation | ✓ | Can convert heightfield patches to mesh |
| Invalid point handling | ✓ | NaN positions handled |
| Level-based queries | ✓ | `getLevelFromResolution` |

### OPC Advanced Patterns

**Folder Validation (PRo3D pattern):**
```fsharp
// Check if directory is valid OPC structure
let isValidOpc (path : string) =
    let hasPatches = OpcPaths.Patches_DirNames |> List.exists (fun n ->
        Directory.Exists(Path.Combine(path, n)))
    let hasHierarchy = File.Exists(Path.Combine(path, "PatchHierarchy.xml"))
    hasPatches && hasHierarchy

// OpcPaths provides standard directory names
// OpcPaths.Patches_DirNames = ["Patches"; "patches"]
// OpcPaths.Images_DirNames = ["Images"; "images"]
```

**Serialization Contract:**
```fsharp
// pickle/unpickle must be inverses for hierarchy caching
let pickle (tree : QTree<Patch>) : byte[] =
    use ms = new MemoryStream()
    BinarySerializer.Serialize(ms, tree)
    ms.ToArray()

let unpickle (bytes : byte[]) : QTree<Patch> =
    use ms = new MemoryStream(bytes)
    BinarySerializer.Deserialize<QTree<Patch>>(ms)

let hierarchy = PatchHierarchy.load pickle unpickle opcPaths
```

**LOD Level Selection:**
```fsharp
// Get patches at specific detail level
let getPatchesAtLevel (hierarchy : PatchHierarchy) (level : int) =
    hierarchy.tree
    |> QTree.flatten
    |> Array.filter (fun patch -> patch.level = level)

// Compute appropriate level from view distance
let level = PatchHierarchy.getLevelFromResolution hierarchy targetTriangleSize
```

**Patch Data Loading:**
```fsharp
// Load all attributes for a patch
let loadPatchData (opcPaths : OpcPaths) (patch : Patch) =
    let patchDir = opcPaths.Patches_DirAbsPath +/ patch.info.Name

    let positions = Aara.fromFile<V3f> (patchDir +/ "Positions.aara")
    let normals =
        let path = patchDir +/ "Normals.aara"
        if File.Exists path then Some (Aara.fromFile<V3f> path) else None
    let colors =
        let path = patchDir +/ "Colors.aara"
        if File.Exists path then Some (Aara.fromFile<C4b> path) else None

    positions, normals, colors
```

---

## Wavefront OBJ Loader

### Namespace
```csharp
using Aardvark.Data.Wavefront;
```

### API

**Load OBJ file:**
```csharp
WavefrontObject ObjParser.Load(string fileName)
WavefrontObject ObjParser.Load(string fileName, Encoding encoding)
WavefrontObject ObjParser.Load(string fileName, bool useDoublePrecision)
```

**Convert to PolyMesh:**
```csharp
List<PolyMesh> WavefrontObject.ToPolyMeshes()
```

### Key Types

**WavefrontObject:**
```csharp
class WavefrontObject
{
    IList Vertices { get; }                    // List<V4f> or List<V4d>
    List<V3f> Normals { get; }
    List<V3f> TextureCoordinates { get; }
    List<WavefrontMaterial> Materials { get; }
    List<string> Groups { get; }

    List<FaceSet> FaceSets { get; }
    List<LineSet> LineSets { get; }
    List<PointSet> PointsSets { get; }
}
```

**FaceSet:**
```csharp
class FaceSet
{
    int GroupIndex { get; }
    List<int> VertexIndices { get; }
    List<int> TexCoordIndices { get; }
    List<int> NormalIndices { get; }
    List<int> MaterialIndices { get; }
    List<int> FirstIndices { get; }
}
```

### Usage
```csharp
// Load OBJ file (MTL loaded automatically from same directory)
var obj = ObjParser.Load(@"C:\models\model.obj");

// Access raw data
int vertexCount = obj.Vertices.Count;
int materialCount = obj.Materials.Count;

// Convert to PolyMesh per group/material
var meshes = obj.ToPolyMeshes();
foreach (var mesh in meshes)
{
    var positions = mesh.PositionArray;   // V3d[]
    var indices = mesh.VertexIndexArray;  // int[]
    var name = mesh.GetProperty<string>(PolyMesh.Property.Name);
    Report.Line($"Mesh '{name}': {positions.Length} verts, {indices.Length / 3} faces");
}
```

### Supported Features
| Feature | Supported | Notes |
|---------|-----------|-------|
| Vertices (v) | ✓ | xyz or xyzw, optional rgb colors |
| Texture coords (vt) | ✓ | uv or uvw |
| Normals (vn) | ✓ | xyz |
| Faces (f) | ✓ | v, v/vt, v//vn, v/vt/vn |
| Lines (l) | ✓ | Line strips |
| Points (p) | ✓ | Point sets |
| Groups (g) | ✓ | Named groups |
| Materials (usemtl) | ✓ | Material assignment |
| MTL files (mtllib) | ✓ | Auto-loaded from same dir |
| Smoothing groups (s) | ✓ | Stored but not used for normals |
| Relative indices | ✓ | Negative indices |
| Line continuation | ✓ | Backslash at line end |

---

## VRML97 Loader

### Namespace
```csharp
using Aardvark.Data.Vrml97;
```

### API

**Load VRML file:**
```csharp
VrmlScene SceneLoader.Load(string filename)
VrmlScene SceneLoader.Load(Vrml97Scene vrmlParseTree)
VrmlScene SceneLoader.Load(Vrml97Scene vrmlParseTree, out Dictionary<SymMapBase, VrmlNode> nodeMap)
```

**Convert to PolyMesh:**
```csharp
PolyMesh PolyMeshFromVrml97.CreateFromIfs(SymMapBase ifs)
PolyMesh PolyMeshFromVrml97.CreateFromIfs(SymMapBase ifs, Options options)
```

### Key Types

**VrmlScene:**
```csharp
class VrmlScene : VrmlGroup
{
    string Name { get; set; }
    string Title { get; set; }
    string[] Info { get; set; }
    List<VrmlRoute> Routes { get; }
    List<VrmlTimeSensor> TimeSensors { get; }
    // Interpolators for animation
}
```

**VrmlGroup/VrmlTransform:**
```csharp
class VrmlGroup : VrmlNode
{
    List<VrmlNode> Children { get; }
}

class VrmlTransform : VrmlGroup
{
    Trafo3d Trafo { get; }
}
```

**VrmlShape:**
```csharp
class VrmlShape : VrmlNode
{
    VrmlGeometry Geometry { get; set; }      // VrmlMesh, VrmlBox, VrmlSphere, etc.
    VrmlAppearance Appearance { get; set; }
}
```

**VrmlMesh:**
```csharp
class VrmlMesh : VrmlGeometry
{
    PolyMesh Mesh { get; }
}
```

### Usage
```csharp
// Load VRML97 scene
var scene = SceneLoader.Load(@"C:\models\scene.wrl");

// Traverse scene graph
void ProcessNode(VrmlNode node, Trafo3d trafo)
{
    if (node is VrmlTransform transform)
    {
        trafo = trafo * transform.Trafo;
        foreach (var child in transform.Children)
            ProcessNode(child, trafo);
    }
    else if (node is VrmlGroup group)
    {
        foreach (var child in group.Children)
            ProcessNode(child, trafo);
    }
    else if (node is VrmlShape shape)
    {
        if (shape.Geometry is VrmlMesh meshGeom)
        {
            var mesh = meshGeom.Mesh;
            var material = shape.Appearance?.Material;
            var texture = shape.Appearance?.Textures;
            // Process mesh with trafo
        }
        else if (shape.Geometry is VrmlBox box)
        {
            // Handle procedural geometry
        }
    }
    else if (node is VrmlPointLight light)
    {
        // Handle lights
    }
}

foreach (var root in scene.Children)
    ProcessNode(root, Trafo3d.Identity);
```

### Conversion Options
```csharp
PolyMeshFromVrml97.Options flags:
- ReverseTriangles           // Flip triangle winding
- AddPerFaceNormals          // Generate flat normals
- AddCreaseNormals           // Generate smooth normals (respects creaseAngle)
- SkipDegenerateFaces        // Remove degenerate triangles
- PreMultiplyTransform       // Apply transform to vertices
- TryFixSpecViolations       // Tolerate spec violations
- IgnorePresentNormals       // Recompute normals even if present
- NoVertexColorsFromMaterial // Don't apply material color to vertices

// Standard usage
var mesh = PolyMeshFromVrml97.CreateFromIfs(ifsNode,
    PolyMeshFromVrml97.Options.StandardSettings);
```

### Supported Features
| Feature | Supported | Notes |
|---------|-----------|-------|
| IndexedFaceSet | ✓ | Primary geometry |
| Box, Sphere, Cone, Cylinder | ✓ | Procedural geometry |
| Transform | ✓ | Translation, rotation, scale |
| Group, Collision | ✓ | Grouping nodes |
| Switch | ✓ | Level-of-detail selection |
| Appearance | ✓ | Material + texture |
| Material | ✓ | Diffuse, specular, emissive |
| Texture | ✓ | ImageTexture, PixelTexture |
| TextureTransform | ✓ | UV transformation |
| Lights | ✓ | Point, spot, directional |
| Routes + Interpolators | ✓ | Animation data preserved |
| DEF/USE | ✓ | Node instancing |
| Inline | ✓ | External file reference |

---

## DGM Loader

### Namespace
```fsharp
open Aardvark.Data.DGM
```

### API

**Load DGM file:**
```fsharp
(DGM * float[]) DGM.loadDgm (fileName : string)
(DGM * float[]) DGM.loadDgmEx (fileName : string) (ci : CultureInfo)
```

**Generate geometry:**
```fsharp
(IndexedGeometry * DGM) DGM.dgm2IndexGeometry (fileName : string)
Matrix<V3f> DGM.computeVertices (dgm : DGM) (arr : float[])
Matrix<V3f> DGM.normals (dgm : DGM) (vertices : Matrix<V3f>)
int[] DGM.createIndex (vertices : Matrix<V3f>)
```

### Key Types

**DGM (header):**
```fsharp
type DGM = {
    ncols        : int
    nrows        : int
    corner       : V2d          // xllcorner, yllcorner
    cellSize     : float
    noDataValue  : float
}
```

### DGM Format
ASCII grid format:
```
NCOLS 100
NROWS 80
XLLCORNER 500000.0
YLLCORNER 6000000.0
CELLSIZE 10.0
NODATA_VALUE -9999.0
<height values, space/newline separated>
```

### Usage
```fsharp
// Load DGM heightfield
let dgm, heightData = DGM.loadDgm @"C:\terrain\elevation.dgm"

printfn "Grid: %d x %d" dgm.ncols dgm.nrows
printfn "Cell size: %f" dgm.cellSize
printfn "Corner: %A" dgm.corner

// Generate triangle mesh
let geometry, dgmInfo = DGM.dgm2IndexGeometry @"C:\terrain\elevation.dgm"

let positions = geometry.IndexedAttributes.[DefaultSemantic.Positions] :?> V3f[]
let normals = geometry.IndexedAttributes.[DefaultSemantic.Normals] :?> V3f[]
let texCoords = geometry.IndexedAttributes.[DefaultSemantic.DiffuseColorCoordinates] :?> V2f[]
let indices = geometry.IndexArray

printfn "Mesh: %d vertices, %d triangles" positions.Length (indices.Length / 3)
```

### Grid to 3D Mapping
- X coordinate = column * cellSize
- Y coordinate = (nrows - row) * cellSize  (inverted)
- Z coordinate = height value
- Invalid heights (noDataValue) become NaN positions (excluded from indices)

### Supported Features
| Feature | Supported | Notes |
|---------|-----------|-------|
| Header parsing | ✓ | NCOLS, NROWS, XLLCORNER, YLLCORNER, CELLSIZE, NODATA_VALUE |
| Height values | ✓ | Space/newline separated floats |
| NaN handling | ✓ | NODATA_VALUE positions set to V3f.NaN |
| Normal generation | ✓ | Central difference method |
| UV generation | ✓ | Normalized grid coordinates |
| Custom CultureInfo | ✓ | For localized number formats |

---

## Photometry Loader

### Namespace
```csharp
using Aardvark.Data.Photometry;
```

### API

**IES format:**
```csharp
IESData IESParser.Parse(string filePath)
IESData ParseMeta(string filePath)  // Header only
```

**LDT format (EULUMDAT):**
```csharp
LDTData LDTParser.Parse(string filePath)
LDTData ParseMeta(string filePath)  // Header only
```

### Key Types

**IESData:**
```csharp
class IESData : LightMeasurementData
{
    IESformat Format { get; }                    // IES1986/1991/1995/2002
    SymbolDict<string> Labels { get; }

    int NumberOfLamps { get; }
    double LumenPerLamp { get; }
    double CandelaMultiplier { get; }
    int VerticalAngleCount { get; }
    int HorizontalAngleCount { get; }
    IESPhotometricType Photometric { get; }      // TypeC, TypeB, TypeA
    IESUnitType Unit { get; }                    // Feet or Meters

    double[] VerticleAngles { get; }             // 0-180 degrees
    double[] HorizontalAngles { get; }           // 0-360 degrees
    Matrix<double> Data { get; }                 // Candela values [vert, horiz]
}
```

**LDTData:**
```csharp
class LDTData : LightMeasurementData
{
    string CompanyName { get; }
    LDTItype Itype { get; }
    LDTSymmetry Symmetry { get; }                // None, Vertical, C0, C1, Quarter
    int PlaneCount { get; }
    double HorAngleStep { get; }
    int ValuesPerPlane { get; }
    double VertAngleStep { get; }

    string LuminaireName { get; }
    int LengthLuminaire { get; }
    int WidthLuminaire { get; }
    int HeightLuminare { get; }

    LDTLampData[] LampSets { get; }
    double[] DirectRatios { get; }

    double[] VerticleAngles { get; }
    double[] HorizontalAngles { get; }
    Matrix<double> Data { get; }                 // Candela values
}
```

### Usage
```csharp
// Load IES file
var iesData = IESParser.Parse(@"C:\lights\spotlight.ies");

Report.Line($"Format: {iesData.Format}");
Report.Line($"Lamps: {iesData.NumberOfLamps} x {iesData.LumenPerLamp} lumens");
Report.Line($"Unit: {iesData.Unit}");
Report.Line($"Grid: {iesData.VerticalAngleCount} x {iesData.HorizontalAngleCount}");

// Access intensity distribution
var vertAngles = iesData.VerticleAngles;   // e.g., [0, 5, 10, ..., 180]
var horizAngles = iesData.HorizontalAngles; // e.g., [0, 90, 180, 270]
var intensities = iesData.Data;             // Matrix: [vertCount, horizCount]

// Sample intensity at specific angles
int vIdx = 18;  // 90 degrees
int hIdx = 0;   // 0 degrees
double candela = intensities[vIdx, hIdx] * iesData.CandelaMultiplier;

// Load LDT (EULUMDAT) file
var ldtData = LDTParser.Parse(@"C:\lights\fixture.ldt");

Report.Line($"Company: {ldtData.CompanyName}");
Report.Line($"Luminaire: {ldtData.LuminaireName}");
Report.Line($"Symmetry: {ldtData.Symmetry}");
Report.Line($"Dimensions: {ldtData.LengthLuminaire} x {ldtData.WidthLuminaire} x {ldtData.HeightLuminare} mm");

foreach (var lamp in ldtData.LampSets)
{
    Report.Line($"  {lamp.Number} x {lamp.Type}: {lamp.TotalFlux} lumens, {lamp.Wattage}W");
}
```

### Coordinate Systems
- **IES Type C** (most common): Vertical angle 0-180° (0=down), Horizontal angle 0-360° (0=reference direction)
- **LDT C-planes**: Vertical angle 0-180° (0=down), Horizontal planes at specified angles
- Intensities in candelas (cd)

### Supported Features
| Feature | IES | LDT | Notes |
|---------|-----|-----|-------|
| Intensity distribution | ✓ | ✓ | Matrix of candela values |
| Labels/metadata | ✓ | ✓ | Manufacturer info, date, etc. |
| Multiple lamps | ✓ | ✓ | Total lumens, wattage |
| Photometric type | ✓ | ✓ | Type A/B/C (IES), Symmetry (LDT) |
| Unit specification | ✓ | ✗ | Feet vs meters (IES only) |
| Luminaire geometry | ✓ | ✓ | Physical dimensions |
| Tilt specification | ✓ | ✓ | TILT data |

---

## Usage Patterns

### Loading a Mesh File

```csharp
// Try multiple formats
PolyMesh LoadMesh(string path)
{
    var ext = Path.GetExtension(path).ToLowerInvariant();

    switch (ext)
    {
        case ".dae":
            var colladaNodes = ColladaImporter.Load(path);
            // Extract first mesh from scene graph
            return FindFirstMesh(colladaNodes);

        case ".obj":
            var obj = ObjParser.Load(path);
            var meshes = obj.ToPolyMeshes();
            return meshes.FirstOrDefault();

        case ".wrl":
            var vrml = SceneLoader.Load(path);
            return FindFirstVrmlMesh(vrml);

        default:
            throw new NotSupportedException($"Format {ext} not supported");
    }
}
```

### Traversing a Scene Graph (glTF)

```fsharp
// Flatten scene to list of (Mesh, Material, Trafo) tuples
let flattenScene (scene : Scene) : list<Mesh * option<Material> * Trafo3d> =
    let rec traverse (trafo : Trafo3d) (node : Node) =
        let nodeTrafo =
            match node.Trafo with
            | Some t -> trafo * t
            | None -> trafo

        // Process this node's meshes
        let meshes =
            node.Meshes |> List.choose (fun meshInst ->
                match Map.tryFind meshInst.Mesh scene.Meshes with
                | Some mesh ->
                    let material =
                        meshInst.Material
                        |> Option.bind (fun matId -> Map.tryFind matId scene.Materials)
                    Some (mesh, material, nodeTrafo)
                | None -> None
            )

        // Recurse children
        let childMeshes = node.Children |> List.collect (traverse nodeTrafo)

        meshes @ childMeshes

    traverse Trafo3d.Identity scene.RootNode
```

### Working with Point Clouds (OPC)

```fsharp
// Load and process OPC point cloud
let processOPC (opcDir : string) =
    let opcPaths = OpcPaths.FromBaseDirectory opcDir

    // Load hierarchy (use provided serialization or implement custom)
    let hierarchy =
        PatchHierarchy.load
            (fun tree -> BinaryPickler.pickle tree)  // Custom serialization
            (fun bytes -> BinaryPickler.unpickle bytes)
            opcPaths

    // Get patches at specific level
    let level = 2
    let patches =
        hierarchy.tree
        |> QTree.flatten
        |> Array.filter (fun p -> p.level = level)

    // Load point data for each patch
    for patch in patches do
        let patchDir = opcPaths.Patches_DirAbsPath +/ patch.info.Name
        let posFile = patchDir +/ "Positions.aara"

        if System.IO.File.Exists posFile then
            let positions = Aara.fromFile<V3f> posFile
            printfn "Patch %s: %d points" patch.info.Name positions.Data.Length

            // Optional: load colors
            let colorFile = patchDir +/ "Colors.aara"
            if System.IO.File.Exists colorFile then
                let colors = Aara.fromFile<C4b> colorFile
                printfn "  with colors: %d" colors.Data.Length
```

### Building Material from Multiple Sources

```csharp
// Combine material data from different formats
class UnifiedMaterial
{
    public C4f BaseColor { get; set; }
    public string DiffuseTexturePath { get; set; }
    public string NormalTexturePath { get; set; }
    public float Roughness { get; set; }
    public float Metallic { get; set; }
}

UnifiedMaterial FromCollada(ColladaMaterial mat)
{
    return new UnifiedMaterial
    {
        BaseColor = mat.Diffuse,
        DiffuseTexturePath = mat.DiffuseColorTexturePath,
        // COLLADA doesn't have PBR params, use defaults
        Roughness = 0.8f,
        Metallic = 0.0f
    };
}

UnifiedMaterial FromGltf(Material mat)
{
    return new UnifiedMaterial
    {
        BaseColor = mat.BaseColor,
        // Textures stored by ImageId, need to resolve
        Roughness = (float)mat.Roughness,
        Metallic = (float)mat.Metallicness
    };
}
```

---

## Gotchas

### 1. Xbim Version Lock (IFC)
**Problem:** IFC loader depends on specific Xbim.Essentials versions. Mismatched versions cause runtime errors.

**Solution:**
```xml
<PackageReference Include="Xbim.Essentials" Version="6.0.0" />
<PackageReference Include="Xbim.Geometry.Engine.Interop" Version="6.0.0" />
```
Check Aardvark.Data.Ifc.csproj for exact versions required.

### 2. F# Interop (GLTF, OPC, DGM)
**Problem:** F# loaders return F# record types and `option<T>`. C# consumers need FSharp.Core.

**Solution:**
```csharp
// Add reference
// <PackageReference Include="FSharp.Core" Version="7.0.0" />

using Microsoft.FSharp.Core;

// Working with options
var scene = GLTF.load(path);
if (FSharpOption<Trafo3d>.get_IsSome(node.Trafo))
{
    var trafo = node.Trafo.Value;
}

// Or use FSharp.Core utilities
var trafo = FSharpOption<Trafo3d>.get_ValueOrDefault(node.Trafo);
```

### 3. Memory for Large Files (IFC, OPC)
**Problem:** IFC preprocessing and OPC hierarchies can consume significant memory.

**Solution:**
```csharp
// IFC: Use single-threaded mode to reduce memory
var ifcData = IFCParser.PreprocessIFC(path, singleThreading: true);

// OPC: Load patches on-demand, don't cache all point data
foreach (var patch in patches.Take(10))  // Process in batches
{
    var positions = Aara.fromFile<V3f>(posFile);
    // Process immediately
    positions = null; // Allow GC
}
```

### 4. Coordinate Systems
**Problem:** Different formats use different conventions:
- **COLLADA**: Y-up (typically)
- **glTF**: Y-up, right-handed
- **IFC**: Z-up (typically)
- **OBJ**: Y-up or Z-up (application-dependent)
- **VRML97**: Y-up, right-handed

**Solution:**
```csharp
// Apply coordinate system transform after loading
Trafo3d YupToZup = Trafo3d.FromBasis(
    V3d.XAxis, V3d.ZAxis, -V3d.YAxis, V3d.Zero
);

// Transform positions
for (int i = 0; i < positions.Length; i++)
    positions[i] = YupToZup.Forward.TransformPos(positions[i]);
```

### 5. Xbim Geometry Engine Threading (IFC)
**Problem:** Xbim geometry engine can deadlock with multi-threading enabled.

**Solution:**
```csharp
// Always use single-threaded mode for IFC
var ifcData = IFCParser.PreprocessIFC(path,
    singleThreading: true,  // ← CRITICAL
    geometryEngine: XGeometryEngineVersion.V6);
```

### 6. OPC Binary Format Sensitivity
**Problem:** .aara files contain type names as strings. Must match exactly.

**Solution:**
```fsharp
// Type names in .aara: "V3f", "V3d", "C4b", "double", "float"
let positions = Aara.fromFile<V3f> posFile  // OK
let positions = Aara.fromFile<V3d> posFile  // Converted automatically if mismatch
```

### 7. DGM Culture-Specific Parsing
**Problem:** DGM files may use locale-specific decimal separators.

**Solution:**
```fsharp
// Default uses InvariantCulture (decimal point)
let dgm, data = DGM.loadDgm path

// For German locale files (comma as decimal separator)
let germanCulture = System.Globalization.CultureInfo("de-DE")
let dgm, data = DGM.loadDgmEx path germanCulture
```

### 8. VRML97 Spec Violations
**Problem:** Many VRML files violate spec (missing data, incorrect counts).

**Solution:**
```csharp
// Use TryFixSpecViolations option
var options = PolyMeshFromVrml97.Options.StandardSettings
            | PolyMeshFromVrml97.Options.TryFixSpecViolations;

var mesh = PolyMeshFromVrml97.CreateFromIfs(ifsNode, options);
```

### 9. PolyMesh vs IndexedGeometry
**Problem:** Different loaders return different types:
- PolyMesh: COLLADA, Wavefront, VRML97, IFC
- Scene (custom): glTF
- IndexedGeometry: DGM

**Solution:** Convert as needed:
```csharp
// PolyMesh → IndexedGeometry
IndexedGeometry ToIndexedGeometry(PolyMesh mesh)
{
    return mesh.ToIndexedGeometry();  // Built-in conversion
}

// glTF Mesh → IndexedGeometry
IndexedGeometry ToIndexedGeometry(Aardvark.Data.GLTF.Mesh mesh)
{
    var ig = new IndexedGeometry
    {
        Mode = mesh.Mode,
        IndexArray = mesh.Index ?? Enumerable.Range(0, mesh.Positions.Length).ToArray(),
    };

    ig.IndexedAttributes[DefaultSemantic.Positions] = mesh.Positions;

    if (mesh.Normals.HasValue)
        ig.IndexedAttributes[DefaultSemantic.Normals] = mesh.Normals.Value;

    // ... copy other attributes

    return ig;
}
```

### 10. Texture Path Resolution
**Problem:** COLLADA and Wavefront use relative/absolute paths. glTF embeds or references.

**Solution:**
```csharp
// COLLADA: Paths relative to .dae file
string ResolvePath(string colladaPath, string texturePath)
{
    if (Path.IsPathRooted(texturePath))
        return texturePath;

    var baseDir = Path.GetDirectoryName(colladaPath);
    return Path.Combine(baseDir, texturePath);
}

// glTF: Extract embedded data
byte[] GetTextureData(Scene scene, ImageId imageId)
{
    if (scene.ImageData.TryGetValue(imageId, out var imageData))
        return imageData.Data;
    return null;
}
```

---

## See Also

- [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md) - Image/texture loading (PixImage formats)
- [TESTING.md](TESTING.md) - Testing format loaders and geometry processing
- [Aardvark.Base](https://github.com/aardvark-platform/aardvark.base) - Core data structures (PolyMesh, V3d, etc.)
- [Aardvark.Rendering](https://github.com/aardvark-platform/aardvark.rendering) - IndexedGeometry and rendering

---

**Document Version:** 1.0
**Generated:** 2025-12-21
**Target Audience:** AI code assistants, developers integrating Aardvark.Data loaders
