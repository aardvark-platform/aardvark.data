# Aardvark.Data.Wavefront

[![Version](https://img.shields.io/nuget/vpre/Aardvark.Data.Wavefront)](https://www.nuget.org/packages/Aardvark.Data.Wavefront/)
[![Downloads](https://img.shields.io/nuget/dt/Aardvark.Data.Wavefront)](https://www.nuget.org/packages/Aardvark.Data.Wavefront/)

Wavefront OBJ/MTL loader for `Aardvark.Base` and `Aardvark.Geometry`.

## Usage

```csharp
using Aardvark.Data.Wavefront;

var obj = ObjParser.Load(@"C:\models\model.obj");

foreach (var fs in obj.FaceSets)
{
    for (int face = 0; face < fs.ElementCount; face++)
    {
        int oi = fs.ObjectIndices[face];
        int mi = fs.MaterialIndices[face];
        var objectName = oi >= 0 ? obj.Objects[oi] : "<none>";
        var materialName = mi >= 0 ? obj.Materials[mi].Name : "<none>";
    }
}

var meshes = obj.GetFaceSetMeshes();
```

## Object and Material Semantics

- `o` is stored per element in `ElementSet.ObjectIndices`.
- `FaceSet.ObjectIndices[i]` maps to `WavefrontObject.Objects[oi]`.
- `oi == -1` means no object was active for that element.
- `usemtl` is stored per face in `FaceSet.MaterialIndices`.
- `FaceSet.MaterialIndices[i]` maps to `WavefrontObject.Materials[mi]`.
- `mi == -1` means no material was active or the material name could not be resolved from the loaded `.mtl` files.
- `g` defines `FaceSet` boundaries. Neither `o` nor `usemtl` splits `FaceSet`s, so a single `FaceSet` can contain faces from multiple objects and materials.

## PolyMesh Projection

`GetFaceSetMeshes()` keeps `PolyMesh.Property.Name` mapped to the OBJ group name. It also projects object and material information when the face set contains at least one resolved object or material:

- `FaceAttributes[WavefrontObject.Property.Objects]` contains the per-face object indices, including `-1` for faces without an active object.
- `FaceAttributes[-WavefrontObject.Property.Objects]` contains the object-name table.
- `InstanceAttributes[WavefrontObject.Property.Objects]` is added when the whole face set belongs to one resolved object.
- `FaceAttributes[PolyMesh.Property.Material]` contains the per-face material indices, including `-1` for unresolved or missing materials.
- `FaceAttributes[-PolyMesh.Property.Material]` contains the material table.
- For the exact raw parser state, inspect `FaceSet.ObjectIndices` and `FaceSet.MaterialIndices` directly.
