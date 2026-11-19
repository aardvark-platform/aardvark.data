### 5.2.36
- VrmlLineSet holds a PolyLineSet (LineSet) built by PolyLineSetFromVrml97.CreateFromIls, colours resolved into LineAttributes or LineVertexAttributes
- Rendering of line sets through PolyLineSet.GetIndexedGeometry(resolveLineAttributes: true), indexed, only vertices shared by differently coloured polylines are split
- BREAKING: removed the array, colour and segment members of VrmlLineSet, ToPolyLineSet and the segment helpers of VrmlHelpers; the binary layout of VrmlLineSet changed
- Requires Aardvark.Geometry.PolyMesh 5.6.6

### 5.2.35
- Update to Aardvark.Geometry.PolyMesh 5.6

### 5.2.34
- Added IndexedLineSet 
- Added wrl tests

### 5.2.33
- Updated to NET 8 and Aardvark.Base 5.3

### 5.2.33-prerelease0001
- Updated to NET 8 and Aardvark.Base 5.3 (prerelease)

### 5.2.32
- Merged with Aardvark.Importer.Vrml97
- Removed AttributeAnnotator
- Cleanup hardcoded strings
- Removed obsolete Parse arguments: annotate, resolveDefUse, duplicateMaps
- Cleanup PolyMesh creating:
    * Removed out-of-spec texture and texCoordTransform processing
    * Restored AddPerFaceNormals and AddCreaseNormals Options
    * Removed PixTexture