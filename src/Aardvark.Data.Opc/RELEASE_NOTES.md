### 0.11.0
- moved Patch to old namespace

### 0.10.0
- moved QTree to old Aardvark.SceneGraph.Opc namespace to prevent serialization issues.

### 0.9.2
- [Prinziple] Added supported for ZIP archives without explicit directory entries
- [Prinziple] Added setAllowedExtensions and added *.opcz as default extension
- [Prinziple] Made handling of paths more robust in regard to directory separators
- [Aara] Added readString
- [Aara] Improved loadRaw

### 0.9.1
- Fixed PatchHierarchy.loadAndCache for ZIPs

### 0.9.0
- Reworked Prinziple
- Removed AutoOpen attribute from Aara module
- Deleted seemingly obsolete stuff from Aara
- Added RequireQualifiedAccess attribute to some union types