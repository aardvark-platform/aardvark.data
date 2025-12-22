# Aardvark.Data Testing Reference

AI-targeted reference for testing in Aardvark.Data.

---

## Test Framework Stack

| Framework | Purpose | Used In |
|-----------|---------|---------|
| NUnit | C# unit tests | CSharpTests, IfcTests, FreeImageTests/UnitTest |
| Expecto | F# unit tests | FSharpTests, PixLoaderTests |
| FsCheck | Property-based testing | PixLoaderTests |
| FsUnit | F# unit test assertions | FSharpTests, PixLoaderTests |
| BenchmarkDotNet | Performance testing | FreeImageTests/Benchmarking, PixLoaderTests |

---

## Running Tests

### All Tests
```bash
dotnet test src/Aardvark.Data.sln
```

### Specific Project
```bash
dotnet test src/Tests/CSharpTests/CSharpTests.csproj
dotnet test src/Tests/FSharpTests/FSharpTests.fsproj
dotnet test src/Tests/IfcTests/IfcTests.csproj
dotnet test src/Tests/PixLoaderTests/PixLoaderTests.fsproj
```

### With Filter
```bash
# Filter by fully qualified name
dotnet test --filter "FullyQualifiedName~GLTF"

# Filter by test name
dotnet test --filter "Name~LoadPrimitive"

# Filter by category/trait
dotnet test --filter "Category=PropertyBased"
```

### Running Benchmarks
Benchmarks are separate executables (not run via `dotnet test`):

```bash
dotnet run --project src/Tests/PixLoaderTests/PixLoaderTests.fsproj -c Release
dotnet run --project src/Tests/FreeImageTests/Benchmarking/Benchmarking.csproj -c Release
```

---

## Test Projects

### CSharpTests (NUnit)
**Location**: `src/Tests/CSharpTests/`
**Framework**: NUnit
**Target**: .NET 8.0

**Structure**:
- `PhotometryTests.cs` - Tests for LDT photometry data parsing and calculations
- Depends on: `Aardvark.Data.Photometry`

**Patterns**:
- Standard NUnit `[TestFixture]` classes
- `[Test]` attribute for test methods
- `[Ignore("reason")]` for tests requiring external data files
- Helper methods for test setup (e.g., `EvaluatePlanes`, `EvaluateZones`)

**Example**:
```csharp
[TestFixture]
public class PhotometryTest
{
    [Test]
    public void GetCPlaneTest()
    {
        var ldtC1 = new LDTData() { /* ... */ };
        var data = new LightMeasurementData(ldt);

        for (int i = 0; i < rows.Length; i++)
        {
            var angle = ldt.HorizontalAngles[i];
            var p = data.GetCPlane(angle);
            Assert.IsTrue(p.First().Item2 == rows[i]);
        }
    }
}
```

---

### FSharpTests (Expecto)
**Location**: `src/Tests/FSharpTests/`
**Framework**: Expecto (run as executable)
**Target**: .NET 8.0

**Structure**:
- `GLTFTests.fs` - GLTF loading, saving, and round-trip tests
- `OpcTests.fs` - OPC (Orthophoto Composite) virtual filesystem tests
- **Embedded Resources**:
  - `data/Avocado.zip`, `data/Avocado.glb` (GLTF test files)
  - `data/2CylinderEngine.gltf`
  - `data/opc/test.zip`, `data/opc/test_implicit_dirs.zip`

**Patterns**:
- Module-based organization
- `[<Test>]` attribute for Expecto tests
- Assembly-embedded test data access via `Assembly.GetManifestResourceStream`
- FsUnit assertions: `|> should equal`, `|> should be True`

**Example**:
```fsharp
module Aardvark.Data.Tests.GLTF

open NUnit.Framework
open Aardvark.Base
open Aardvark.Data.GLTF

[<Test>]
let ``GLTF.readFrom working with GLB``() =
    let name = selfAssembly.GetManifestResourceNames() |> Array.find (fun n -> n.EndsWith "Avocado.glb")
    use s = selfAssembly.GetManifestResourceStream(name)
    GLTF.readFrom s |> ignore

[<Test>]
let ``GLTF.roundtrip GLB``() =
    testScene |> GLTF.toArray |> GLTF.ofArray |> ignore
```

---

### IfcTests (NUnit)
**Location**: `src/Tests/IfcTests/`
**Framework**: NUnit
**Target**: .NET Framework 4.7.2 + .NET 8.0 (multi-targeting)

**Structure**:
- `IfcTests.cs` - IFC (Industry Foundation Classes) loading and export tests
- **Embedded Resources**: 6 `.ifc` test files in `data/` folder
  - `simple_scene.ifc`, `slab.ifc`, `surface-model.ifc`, `test_Material.ifc`, `Viadotto Acerno_ifc43.ifc`, `wall.ifc`

**Patterns**:
- `LoadEmbeddedData` helper extracts embedded resources to temp files
- Test fixtures: `LoadingTest` and `ExportTest`
- Static test methods
- Tests create IFC models programmatically and validate against schema

**Example**:
```csharp
[TestFixture]
public static class LoadingTest
{
    [Test]
    public static void LoadPrimitive()
    {
        LoadEmbeddedData(@"data\surface-model.ifc", (filePath) => {
            var parsed = IFCParser.PreprocessIFC(filePath);
            Assert.AreEqual(1, parsed.Materials.Count);
        });
    }
}
```

**Gotcha**: Embedded resources must match pattern `{AssemblyName}.{path}` - uses Regex to convert paths.

---

### PixLoaderTests (Expecto + FsCheck + Benchmarks)
**Location**: `src/Tests/PixLoaderTests/`
**Framework**: Expecto, FsCheck (property-based), BenchmarkDotNet
**Target**: .NET 8.0 **Windows-only** (`net8.0-windows10.0.17763.0`)

**Structure**:
- `PixLoaderTests.fs` - Property-based tests for image loaders
- `Pfim/Dds.fs`, `Pfim/Tga.fs` - Pfim loader tests
- `TiffTests.fs` - TIFF-specific tests
- `Benchmark.fs` - BenchmarkDotNet performance tests
- `Program.fs` - Entry point for Expecto runner
- **Embedded Resources**: 50+ DDS/TGA test images in `data/` folder

**Patterns**:
- Property-based testing with custom `Arb.fromGen` generators
- `[<Property(Arbitrary = [| typeof<Generator> |])>]` attribute
- FsCheck generates random test cases (image sizes, formats, loaders)
- Benchmarks compare DevIL, FreeImage, ImageSharp loaders

**Example (Property-Based)**:
```fsharp
type SaveLoadInput =
    {
        Image       : PixImage<byte>
        SaveParams  : PixSaveParams
        Encoder     : IPixLoader
        Decoder     : IPixLoader
        UseStream   : bool
    }

type Generator private () =
    static member SaveLoadInput =
        gen {
            let! cf, iff = Gen.colorAndImageFileFormat
            let! pix = Gen.checkerboardPix cf
            let! useStream = Gen.elements [false; true]
            let! encoder = Gen.pixEncoder useStream iff
            let! decoder = Gen.pixDecoder useStream iff
            // ...
        }
        |> Arb.fromGen

[<Property(Arbitrary = [| typeof<Generator> |])>]
let ``[PixLoader] Save and load`` (input : SaveLoadInput) =
    tempFile (fun file ->
        let output =
            if input.UseStream then
                use stream = File.Open(file, FileMode.Create, FileAccess.ReadWrite)
                input.Image.Save(stream, input.SaveParams, input.Encoder)
                stream.Position <- 0L
                PixImage<byte>(stream, input.Decoder)
            else
                input.Image.Save(file, input.SaveParams, false, input.Encoder)
                PixImage<byte>(file, input.Decoder)

        PixImage.compare input.Image output
    )
```

**Example (Benchmark)**:
```fsharp
[<MemoryDiagnoser>]
type LoaderBenchmark() =
    [<DefaultValue; Params(256, 1024, 2048, 4096)>]
    val mutable Size : int

    [<DefaultValue; Params(PixFileFormat.Png, PixFileFormat.Jpeg, PixFileFormat.Tiff)>]
    val mutable Format : PixFileFormat

    [<Benchmark(Description = "Load (ImageSharp)")>]
    member x.Load_ImageSharp() =
        PixImage.Load(filename, PixImageSharp.Loader) |> ignore
```

---

### FreeImageTests
**Location**: `src/Tests/FreeImageTests/`
**Two Subprojects**:

#### UnitTest (NUnit)
**Framework**: NUnit
**Target**: .NET 8.0
**Structure**:
- `TestFixtures/` - FreeImageBitmapTest, ImportedStructsTest, etc.
- `SetUpFixture.cs` - Test setup
- Tests FreeImage library wrappers

#### Benchmarking (BenchmarkDotNet)
**Framework**: BenchmarkDotNet
**Target**: .NET 8.0
**Structure**:
- `Benchmarks/StreamReadBenchmark.cs`, `Benchmarks/StreamWriteBenchmark.cs`
- `Program.cs` - BenchmarkDotNet runner
- Copies native FreeImage.dll to output

---

## Adding New Tests

### C# (NUnit Pattern)
```csharp
using NUnit.Framework;

namespace Aardvark.Data.Tests
{
    [TestFixture]
    public class MyNewTests
    {
        [Test]
        public void TestSomething()
        {
            var result = SomeOperation();
            Assert.AreEqual(expectedValue, result);
        }

        [Test, Ignore("Requires external data")]
        public void TestWithExternalData()
        {
            // Test requiring data files not in repo
        }
    }
}
```

### F# (Expecto Pattern)
```fsharp
module MyNewTests

open NUnit.Framework
open FsUnit
open Expecto

[<Test>]
let ``should do something``() =
    let result = someOperation()
    result |> should equal expectedValue

[<Test>]
let ``should handle edge case``() =
    (fun () -> dangerousOperation()) |> should throw typeof<ArgumentException>
```

### Property-Based (FsCheck)
```fsharp
type MyInput = { Value : int; Name : string }

type Generator private () =
    static member MyInput =
        gen {
            let! v = Gen.choose (1, 100)
            let! n = Gen.elements ["A"; "B"; "C"]
            return { Value = v; Name = n }
        }
        |> Arb.fromGen

[<Property(Arbitrary = [| typeof<Generator> |])>]
let ``property holds for all inputs`` (input : MyInput) =
    let result = processInput input
    result.Value |> should be (greaterThan 0)
```

### Benchmark (BenchmarkDotNet)
```fsharp
open BenchmarkDotNet.Attributes

[<MemoryDiagnoser>]
type MyBenchmark() =
    [<DefaultValue; Params(10, 100, 1000)>]
    val mutable Size : int

    [<GlobalSetup>]
    member x.Setup() =
        // Initialize test data
        ()

    [<Benchmark>]
    member x.MethodA() =
        // Code to benchmark
        ()
```

---

## Test Data

### Embedded Resources
**How to Embed**:
1. Add file to project (e.g., `data/test.ifc`)
2. Set Build Action to `EmbeddedResource` in `.csproj`:
   ```xml
   <EmbeddedResource Include="data/test.ifc" />
   ```

**How to Access**:
```csharp
// C#
var asm = Assembly.GetExecutingAssembly();
var name = "AssemblyName.data.test.ifc";
using var stream = asm.GetManifestResourceStream(name);
```

```fsharp
// F#
let asm = typeof<Marker>.Assembly
let name = asm.GetManifestResourceNames() |> Array.find (fun n -> n.EndsWith "test.glb")
use stream = asm.GetManifestResourceStream(name)
```

### Test File Locations
- **GLTF**: `src/Tests/FSharpTests/data/*.{gltf,glb,zip}`
- **OPC**: `src/Tests/FSharpTests/data/opc/*.zip`
- **IFC**: `src/Tests/IfcTests/data/*.ifc`
- **Images (DDS/TGA)**: `src/Tests/PixLoaderTests/data/*.{dds,tga}`

---

## Gotchas

1. **PixLoaderTests is Windows-only**: Target framework `net8.0-windows10.0.17763.0` - uses Windows Media APIs
2. **Native DLLs**: FreeImage tests require `FreeImage.dll` in output directory
3. **Expecto vs NUnit**:
   - Expecto tests run as executables (`OutputType=Exe`)
   - NUnit tests run via `dotnet test`
   - Don't mix frameworks in same project
4. **Embedded resources**:
   - Must set `<EmbeddedResource Include="..."/>` in project file
   - Resource names use dots instead of slashes: `AssemblyName.data.file.ext`
5. **Property-based tests**:
   - Can be slow with default iteration count
   - Use `[<Property(MaxTest = 10)>]` to limit iterations during development
6. **Multi-targeting (IfcTests)**: Tests run on both .NET Framework 4.7.2 and .NET 8.0
7. **Benchmarks need Release mode**: `dotnet run -c Release` for accurate results
8. **FsCheck generators**: Must be provided via `Arbitrary` parameter, not auto-discovered
9. **Test data paths**: Use `Path.Combine` or `Path.combine` (F#), never hardcoded `\` or `/`
10. **Temp files**: Always clean up in `finally` blocks or use helpers like `tempFile`

---

## See Also

- [PIXIMAGE_LOADERS.md](PIXIMAGE_LOADERS.md) - Image loader APIs and usage
- [DATA_FORMATS.md](DATA_FORMATS.md) - Format loader APIs (GLTF, IFC, OPC, Photometry)
- [AGENTS.md](../AGENTS.md) - Build commands and project structure
