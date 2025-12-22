# AI Agent Guide

## Tips for AI Agents

1. **Start with** `ai/QUICK_REF.md` for one-liner APIs (~100 lines)
2. **Prefer ImageSharp** for new image loading code (pure C#, cross-platform)
3. **F# for hierarchical formats**: GLTF, OPC, DGM use F# for type safety
4. **Check platform**: SystemDrawing and WindowsMedia are Windows-only
5. **Paket groups**: Test dependencies are in `group Test` section
6. **Native binaries**: FreeImage requires copying native DLLs
7. **Run tests per-project**: `dotnet test src/Tests/<Project>`
8. **OPC workflows**: Validate folder structure before loading (`Patches/`, `Images/`, `PatchHierarchy.xml`)
9. **IFC threading**: Always use `singleThreading: true` to avoid Xbim deadlocks
10. **Multi-format apps**: See plugin loader pattern in QUICK_REF.md

## AI Documentation Index

| Document | Purpose | Size |
|----------|---------|------|
| [ai/QUICK_REF.md](ai/QUICK_REF.md) | One-liner APIs, cheat sheet | ~100 lines |
| [ai/README.md](ai/README.md) | Full index with line ranges | ~100 lines |
| [ai/PIXIMAGE_LOADERS.md](ai/PIXIMAGE_LOADERS.md) | Image loading APIs | ~520 lines |
| [ai/DATA_FORMATS.md](ai/DATA_FORMATS.md) | 3D/geometry format loaders | ~1280 lines |
| [ai/TESTING.md](ai/TESTING.md) | Testing patterns | ~420 lines |

## Supported Commands

| Task | Command | Notes |
|------|---------|-------|
| Restore | `./build.sh` or `.\build.cmd` | Restores packages via Paket + builds |
| Build | `./build.sh` or `.\build.cmd` | Builds entire solution |
| Build one | `dotnet build src/<Project>/<Project>.csproj` | Single project |
| Test | `dotnet test src/Tests/<TestProject>` | Run specific test project |

## Dependency Management

This project uses **Paket** for dependency management.

| Task | Command |
|------|---------|
| Restore packages | `dotnet paket restore` |
| Add package | Edit `paket.dependencies`, run `dotnet paket install` |
| Update package | `dotnet paket update <PackageName>` |

**Rules:**
- Never edit `paket.lock` manually
- Add dependencies to `paket.dependencies` at root
- Test dependencies go in `group Test`
- Use `~>` for stable version ranges

## File Ownership by Change Type

| Change Type | Files to Modify | Files to NOT Touch |
|-------------|-----------------|-------------------|
| Add feature | `src/<Project>/**/*.cs` or `*.fs` | Other projects, test data |
| Add test | `src/Tests/<TestProject>/**/*` | Source files in other projects |
| Fix bug | Relevant source + test | Unrelated modules |
| Add format | New project under `src/` | Existing loaders |

## Framework & SDK Rules

- **.NET Version**: 8.0.0 (see `global.json`, rollForward: latestFeature)
- **Target Frameworks**:
  - Libraries: `netstandard2.0` (cross-platform) or `net8.0`
  - Tests: `net8.0` or `net8.0-windows10.0.17763.0`
- **C# LangVersion**: 12
- **F# Projects**: Explicit FSharp.Core >= 8.0.0

## Common Failure Modes & Fixes

| Symptom | Cause | Fix |
|---------|-------|-----|
| Package restore fails | Paket not restored | Run `dotnet tool restore` then `dotnet paket restore` |
| Native DLL not found | FreeImage.dll missing | Check `lib/Native/` copied to output |
| Windows-only test fails | Platform-specific code | Run on Windows or skip with `[<Platform>]` |
| Xbim version conflict | Locked versions | Do not change Xbim versions in paket.dependencies |

## Project Structure

```
src/
├── Aardvark.Data.*/        # Data format loaders (8 projects)
│   ├── Collada/            # COLLADA .dae (C#)
│   ├── GLTF/               # glTF 2.0 (F#)
│   ├── Ifc/                # IFC building models (C#)
│   ├── Opc/                # Point cloud hierarchies (F#)
│   ├── DGM/                # DGM format (F#)
│   ├── Photometry/         # Lighting data (C#)
│   ├── Vrml97/             # VRML97 scenes (C#)
│   └── Wavefront/          # OBJ meshes (C#)
├── Aardvark.PixImage.*/    # Image loaders (6 projects)
│   ├── DevIL/              # DevIL library (C#)
│   ├── FreeImage/          # FreeImage library (C#)
│   ├── ImageSharp/         # Pure C# - preferred (F#)
│   ├── Pfim/               # DDS/TGA specialist (F#)
│   ├── SystemDrawing/      # Windows GDI+ (C#)
│   └── WindowsMedia/       # Windows Media (C#)
├── FreeImage.Standard/     # P/Invoke wrapper for FreeImage
├── Tests/
│   ├── CSharpTests/        # General C# tests (NUnit)
│   ├── FSharpTests/        # GLTF, OPC tests (Expecto)
│   ├── IfcTests/           # IFC tests (NUnit)
│   └── PixLoaderTests/     # Image loader tests (Expecto)
└── Aardvark.Data.sln
```

