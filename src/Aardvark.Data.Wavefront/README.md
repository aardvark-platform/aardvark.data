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
        int mi = fs.MaterialIndices[face];
        var materialName = mi >= 0 ? obj.Materials[mi].Name : "<none>";
    }
}

var meshes = obj.GetFaceSetMeshes();
```

## Material Semantics

- `usemtl` is stored per face in `FaceSet.MaterialIndices`.
- `FaceSet.MaterialIndices[i]` maps to `WavefrontObject.Materials[mi]`.
- `mi == -1` means no material was active or the material name could not be resolved from the loaded `.mtl` files.
- `usemtl` does not split `FaceSet`s. `FaceSet` boundaries follow `g` group changes, so a single `FaceSet` can contain multiple materials.

## PolyMesh Projection

`GetFaceSetMeshes()` exposes material information on the resulting `PolyMesh`:

- `FaceAttributes[PolyMesh.Property.Material]` contains the per-face material indices.
- `FaceAttributes[-PolyMesh.Property.Material]` contains the material table.
- For the exact raw parser state, inspect `FaceSet.MaterialIndices` directly.
