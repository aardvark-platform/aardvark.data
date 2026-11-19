### 1.1.4
- Added assembly extensions: part hierarchies via IfcRelAggregates, snap positions via IfcRelConnectsElements and IfcPointOnCurve
- Import curve representations ('Axis', 'Annotation', 'FootPrint', curve items in 'Body') as PolyLineSet entries (IFCContent.LineSet, IFCData.GetLineSets); curve style colours in LineAttributes[Colors]
- Added tracks as IfcCableSegment (BUSBARSEGMENT) with IfcCableSegmentType, snap curve as 'Axis', AttachToTrack by curve parameter (TrackExt)
- Added oriented mounting points as IfcDistributionPort with MountAtPort and an optional frame marker annotation (PortExt)
- IfcColourRgb converts to C3b
- Requires Aardvark.Geometry.PolyMesh 5.6.6

### 1.1.3
- Fixed placement conversions and TryGetSimpleValue<long>
- Added enableCaching / EndCaching to allow editing imported models
- Extensions work on cross-schema interfaces

### 1.1.2
- Extended polylines
- Added axis shapes representations

### 1.1.1
- Update to .NET 10
- Update to Aardvark.Geometry.PolyMesh 5.6

### 1.1.0
- Update xBIM-libs and enable access of IfcStore (pinned MeasuredEsent to 2.0.3)

### 1.1.0-prerelease0013
- Restore missing extension

### 1.1.0-prerelease0012
- Update xBIM-libs and clean up redundant functionality

### 1.1.0-prerelease0011
- Fixed project length unit scale

### 1.1.0-prerelease0010
- Extend IFC cross schema support

### 1.1.0-prerelease0009
- Fixed broken build

### 1.1.0-prerelease0008
- Refactored extensions to support cross schema creation

### 1.1.0-prerelease0007
- Updated xBIM-Essentials to resolve reported bug and added IfcLightingFixture Extensions 

### 1.1.0-prerelease0006
- Restored features from v6-branch

### 1.1.0-prerelease0005
- Updated to xBIM-Geometry 6.1.801-netcore and xBIM-Essentials 6.0.489

### 1.0.4
- Updated to NET 8 and Aardvark.Base 5.3

### 1.0.4-prerelease0001
- Updated to NET 8 and Aardvark.Base 5.3 (prerelease)

### 1.0.3
- Updated Aardvark.Base packages

### 1.0.2
- Relaxed package version constraints

### 1.0.1
- Added explicit Xbim package references

### 1.0.0
- Initial package (moved from internal)