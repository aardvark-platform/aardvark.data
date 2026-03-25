module Aardvark.Data.Tests.GLTF

open System
open System.IO
open System.IO.Compression
open System.Text
open NUnit.Framework
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.Data.GLTF

[<AutoOpen>]
module StreamExtensions =

    type System.IO.Stream with
        member x.ReadToEnd() =
            let remSize =
                try
                    let len = x.Length
                    let pos = x.Position
                    int (len - pos)
                with _ ->
                    1 <<< 20

            let mutable arr = Array.zeroCreate<byte> remSize

            let mutable o = 0
            let mutable rem = arr.Length
            let mutable finished = false
            while not finished do
                if rem < o then
                    System.Array.Resize(&arr, arr.Length <<< 1)
                    rem <- arr.Length - o

                let r = x.Read(arr, o, rem)
                if r = 0 then
                    finished <- true
                else
                    rem <- rem - r
                    o <- o + r

            if o < arr.Length then
                System.Array.Resize(&arr, o)
            arr

type NonSeekableStream(inner : Stream) =
    inherit Stream()

    override _.CanRead = inner.CanRead
    override _.CanSeek = false
    override _.CanWrite = inner.CanWrite
    override _.Length = raise (NotSupportedException())
    override _.Position with get() = raise (NotSupportedException()) and set _ = raise (NotSupportedException())
    override _.Flush() = inner.Flush()
    override _.Read(buffer, offset, count) = inner.Read(buffer, offset, count)
    override _.Seek(_, _) = raise (NotSupportedException())
    override _.SetLength(_) = raise (NotSupportedException())
    override _.Write(buffer, offset, count) = inner.Write(buffer, offset, count)

    override this.Dispose(disposing : bool) =
        if disposing then inner.Dispose()
        base.Dispose(disposing)

module private TestData =

    let tinyPng =
        Convert.FromBase64String "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+tmc8AAAAASUVORK5CYII="

    let toDataUri (mime : string) (data : byte[]) =
        sprintf "data:%s;base64,%s" mime (Convert.ToBase64String data)

    let writeBytes (write : BinaryWriter -> unit) =
        use stream = new MemoryStream()
        use writer = new BinaryWriter(stream, Encoding.UTF8, true)
        write writer
        writer.Flush()
        stream.ToArray()

    let bytesOfFloat32s (values : float32[]) =
        writeBytes (fun writer -> values |> Array.iter writer.Write)

    let bytesOfUInt16s (values : uint16[]) =
        writeBytes (fun writer -> values |> Array.iter writer.Write)

    let bytesOfUInt8s (values : byte[]) =
        Array.copy values

    let buildBuffer (segments : list<int * byte[]>) =
        use stream = new MemoryStream()

        let ranges =
            [|
                for alignment, data in segments do
                    while stream.Position % int64 alignment <> 0L do
                        stream.WriteByte 0uy

                    let offset = int stream.Position
                    stream.Write(data, 0, data.Length)
                    yield offset, data.Length
            |]

        stream.ToArray(), ranges

type Marker = Marker
let selfAssembly = typeof<Marker>.Assembly

let private resourceStream (suffix : string) =
    let name = selfAssembly.GetManifestResourceNames() |> Array.find (fun n -> n.EndsWith suffix)
    selfAssembly.GetManifestResourceStream(name)

let private resourceBytes (suffix : string) =
    use stream = resourceStream suffix
    stream.ReadToEnd()

let private expectSingleMesh (scene : Scene) =
    Assert.That(scene.Meshes.Count, Is.EqualTo 1)
    let (KeyValue(_, mesh)) = scene.Meshes |> Seq.exactlyOne
    mesh

let private expectSingleImage (scene : Scene) =
    Assert.That(scene.ImageData.Count, Is.EqualTo 1)
    let (KeyValue(_, image)) = scene.ImageData |> Seq.exactlyOne
    image

let private expectColors (scene : Scene) =
    let mesh = expectSingleMesh scene
    match mesh.Colors with
    | Some colors -> colors
    | None -> Assert.Fail "expected mesh colors"; [||]

let testScene =
    let mutable materials = Map.empty
    let mutable geometries = Map.empty
    let mutable nodes = []

    let gid =
        let pos = [| V3f.Zero; V3f.ZAxis; V3f.XAxis |]
        let idx = [| 0; 1; 2 |]

        let geometry =
            {
                Name            = None
                BoundingBox     = Box3f.FromCenterAndSize(V3f.Zero, V3f.III * 0.6f) |> Box3d
                Mode            = IndexedGeometryMode.TriangleList
                Index           = Some idx
                Positions       = pos
                Normals         = Some (pos |> Array.map Vec.normalize)
                Tangents        = None
                TexCoords       = []
                Colors          = None
            }

        let gid = MeshId.New()
        geometries <- Map.add gid geometry geometries
        gid

    let steps = 8
    for ri in 0 .. steps - 1 do
        let mutable roughness = float ri / float (steps - 1) |> float32 |> float
        for mi in 0 .. steps - 1 do
            let mutable metalness = float mi / float (steps - 1) |> float32 |> float
            let offset = Trafo3d.Translation(float ri, float mi, 0.0)

            let mid = MaterialId.New()

            let material =
                {
                    Name                = Some (sprintf "%.3f_%.3f" roughness metalness)

                    DoubleSided         = true
                    Opaque              = true

                    BaseColorTexture    = None
                    BaseColor           = C4f.Beige

                    Roughness           = roughness
                    RoughnessTexture    = None
                    RoughnessTextureComponent = 1

                    Metallicness        = metalness
                    MetallicnessTexture = None
                    MetallicnessTextureComponent = 2

                    EmissiveColor       = C4f.Black
                    EmissiveTexture     = None

                    NormalTexture       = None
                    NormalTextureScale  = 1.0
                }

            materials <- Map.add mid material materials
            nodes <- { Name = None; Trafo = Some offset; Meshes = [ { Mesh = gid; Material = Some mid } ]; Children = [] } :: nodes

    {
        Materials = materials
        Meshes = geometries
        ImageData = Map.empty
        RootNode = { Name = None; Trafo = None; Meshes = []; Children = nodes }
    }

let private createColorFixture (componentType : int) (attributeType : string) (normalized : bool) (colorData : byte[]) =
    let positions = TestData.bytesOfFloat32s [| 0.0f; 0.0f; 0.0f; 1.0f; 0.0f; 0.0f; 0.0f; 1.0f; 0.0f |]
    let buffer, ranges = TestData.buildBuffer [ 4, positions; 4, colorData ]
    let posOffset, posLength = ranges.[0]
    let colorOffset, colorLength = ranges.[1]

    sprintf """
{
  "asset": { "version": "2.0" },
  "buffers": [
    { "byteLength": %d, "uri": "%s" }
  ],
  "bufferViews": [
    { "buffer": 0, "byteOffset": %d, "byteLength": %d },
    { "buffer": 0, "byteOffset": %d, "byteLength": %d }
  ],
  "accessors": [
    { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3", "min": [0, 0, 0], "max": [1, 1, 0] },
    { "bufferView": 1, "componentType": %d, "count": 3, "type": "%s", "normalized": %s }
  ],
  "meshes": [
    { "primitives": [ { "attributes": { "POSITION": 0, "COLOR_0": 1 } } ] }
  ],
  "nodes": [ { "mesh": 0 } ],
  "scenes": [ { "nodes": [0] } ],
  "scene": 0
}
"""
        buffer.Length
        (TestData.toDataUri "application/octet-stream" buffer)
        posOffset
        posLength
        colorOffset
        colorLength
        componentType
        attributeType
        (if normalized then "true" else "false")

let private createTexCoordFixture() =
    let positions = TestData.bytesOfFloat32s [| 0.0f; 0.0f; 0.0f; 1.0f; 0.0f; 0.0f; 0.0f; 1.0f; 0.0f |]
    let texCoord1 = TestData.bytesOfFloat32s [| 0.25f; 0.50f; 0.75f; 0.50f; 0.25f; 0.00f |]
    let texCoord0 = TestData.bytesOfFloat32s [| 0.00f; 0.00f; 1.00f; 0.00f; 0.00f; 1.00f |]
    let buffer, ranges = TestData.buildBuffer [ 4, positions; 4, texCoord1; 4, texCoord0 ]
    let posOffset, posLength = ranges.[0]
    let tc1Offset, tc1Length = ranges.[1]
    let tc0Offset, tc0Length = ranges.[2]

    sprintf """
{
  "asset": { "version": "2.0" },
  "buffers": [
    { "byteLength": %d, "uri": "%s" }
  ],
  "bufferViews": [
    { "buffer": 0, "byteOffset": %d, "byteLength": %d },
    { "buffer": 0, "byteOffset": %d, "byteLength": %d },
    { "buffer": 0, "byteOffset": %d, "byteLength": %d }
  ],
  "accessors": [
    { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3", "min": [0, 0, 0], "max": [1, 1, 0] },
    { "bufferView": 1, "componentType": 5126, "count": 3, "type": "VEC2" },
    { "bufferView": 2, "componentType": 5126, "count": 3, "type": "VEC2" }
  ],
  "images": [
    { "uri": "%s" }
  ],
  "textures": [
    { "source": 0 }
  ],
  "materials": [
    {
      "pbrMetallicRoughness": {
        "baseColorTexture": { "index": 0, "texCoord": 1 }
      }
    }
  ],
  "meshes": [
    {
      "primitives": [
        {
          "attributes": {
            "TEXCOORD_1": 1,
            "POSITION": 0,
            "TEXCOORD_0": 2
          },
          "material": 0
        }
      ]
    }
  ],
  "nodes": [ { "mesh": 0 } ],
  "scenes": [ { "nodes": [0] } ],
  "scene": 0
}
"""
        buffer.Length
        (TestData.toDataUri "application/octet-stream" buffer)
        posOffset
        posLength
        tc1Offset
        tc1Length
        tc0Offset
        tc0Length
        (TestData.toDataUri "image/png" TestData.tinyPng)

let private createArchive (entries : list<string * Choice<string, byte[]>>) =
    use output = new MemoryStream()
    use archive = new ZipArchive(output, ZipArchiveMode.Create, true)

    let writeEntry (path : string) (bytes : byte[]) =
        let entry = archive.CreateEntry(path)
        use stream = entry.Open()
        stream.Write(bytes, 0, bytes.Length)

    let writeTextEntry (path : string) (text : string) =
        let entry = archive.CreateEntry(path)
        use stream = entry.Open()
        use writer = new StreamWriter(stream, Encoding.UTF8)
        writer.Write(text)

    for path, content in entries do
        match content with
        | Choice1Of2 text -> writeTextEntry path text
        | Choice2Of2 bytes -> writeEntry path bytes

    archive.Dispose()
    output.ToArray()

let private createNestedZipFixture() =
    let positions = TestData.bytesOfFloat32s [| 0.0f; 0.0f; 0.0f; 1.0f; 0.0f; 0.0f; 0.0f; 1.0f; 0.0f |]

    let gltf =
        sprintf """
{
  "asset": { "version": "2.0" },
  "buffers": [
    { "byteLength": %d, "uri": "buffers/triangle.bin" }
  ],
  "bufferViews": [
    { "buffer": 0, "byteOffset": 0, "byteLength": %d }
  ],
  "accessors": [
    { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3", "min": [0, 0, 0], "max": [1, 1, 0] }
  ],
  "images": [
    { "uri": "textures/baseColor.png" }
  ],
  "meshes": [
    { "primitives": [ { "attributes": { "POSITION": 0 } } ] }
  ],
  "nodes": [ { "mesh": 0 } ],
  "scenes": [ { "nodes": [0] } ],
  "scene": 0
}
"""
            positions.Length
            positions.Length

    createArchive [
        "nested/scene.gltf", Choice1Of2 gltf
        "nested/buffers/triangle.bin", Choice2Of2 positions
        "nested/textures/baseColor.png", Choice2Of2 TestData.tinyPng
    ]

let private createParentRelativeZipFixture() =
    let positions = TestData.bytesOfFloat32s [| 0.0f; 0.0f; 0.0f; 1.0f; 0.0f; 0.0f; 0.0f; 1.0f; 0.0f |]

    let gltf =
        sprintf """
{
  "asset": { "version": "2.0" },
  "buffers": [
    { "byteLength": %d, "uri": "../buffers/triangle.bin" }
  ],
  "bufferViews": [
    { "buffer": 0, "byteOffset": 0, "byteLength": %d }
  ],
  "accessors": [
    { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3", "min": [0, 0, 0], "max": [1, 1, 0] }
  ],
  "images": [
    { "uri": "../textures/baseColor.png" }
  ],
  "meshes": [
    { "primitives": [ { "attributes": { "POSITION": 0 } } ] }
  ],
  "nodes": [ { "mesh": 0 } ],
  "scenes": [ { "nodes": [0] } ],
  "scene": 0
}
"""
            positions.Length
            positions.Length

    createArchive [
        "nested/models/scene.gltf", Choice1Of2 gltf
        "nested/buffers/triangle.bin", Choice2Of2 positions
        "nested/textures/baseColor.png", Choice2Of2 TestData.tinyPng
    ]

let private createEscapingZipFixture() =
    let positions = TestData.bytesOfFloat32s [| 0.0f; 0.0f; 0.0f; 1.0f; 0.0f; 0.0f; 0.0f; 1.0f; 0.0f |]

    let gltf =
        sprintf """
{
  "asset": { "version": "2.0" },
  "buffers": [
    { "byteLength": %d, "uri": "%s" }
  ],
  "bufferViews": [
    { "buffer": 0, "byteOffset": 0, "byteLength": %d }
  ],
  "accessors": [
    { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3", "min": [0, 0, 0], "max": [1, 1, 0] }
  ],
  "images": [
    { "uri": "../textures/baseColor.png" }
  ],
  "meshes": [
    { "primitives": [ { "attributes": { "POSITION": 0 } } ] }
  ],
  "nodes": [ { "mesh": 0 } ],
  "scenes": [ { "nodes": [0] } ],
  "scene": 0
}
"""
            positions.Length
            (TestData.toDataUri "application/octet-stream" positions)
            positions.Length

    createArchive [
        "scene.gltf", Choice1Of2 gltf
        "textures/baseColor.png", Choice2Of2 TestData.tinyPng
    ]

let private createRoundtripScene() =
    let imageId = ImageId.New()
    let materialId = MaterialId.New()
    let meshId = MeshId.New()

    let positions =
        [|
            V3f.Zero
            V3f.XAxis
            V3f.YAxis
        |]

    let texCoord0 =
        [|
            V2f(0.0f, 0.0f)
            V2f(1.0f, 0.0f)
            V2f(0.0f, 1.0f)
        |]

    let texCoord1 =
        [|
            V2f(0.25f, 0.75f)
            V2f(0.75f, 0.75f)
            V2f(0.25f, 0.25f)
        |]

    let colors =
        [|
            C4b(255, 0, 0, 255)
            C4b(0, 128, 0, 128)
            C4b(0, 0, 64, 64)
        |]

    let material =
        {
            Name                         = Some "textured"
            DoubleSided                  = true
            Opaque                       = true
            BaseColorTexture             = Some imageId
            BaseColor                    = C4f(1.0f, 1.0f, 1.0f, 1.0f)
            Roughness                    = 1.0
            RoughnessTexture             = None
            RoughnessTextureComponent    = 1
            Metallicness                 = 0.0
            MetallicnessTexture          = None
            MetallicnessTextureComponent = 2
            EmissiveColor                = C4f(0.0f, 0.0f, 0.0f, 0.0f)
            EmissiveTexture              = None
            NormalTexture                = None
            NormalTextureScale           = 1.0
        }

    let mesh =
        {
            Name        = Some "triangle"
            BoundingBox = Box3d(V3d.Zero, V3d(1.0, 1.0, 0.0))
            Mode        = IndexedGeometryMode.TriangleList
            Index       = Some [| 0; 1; 2 |]
            Positions   = positions
            Normals     = Some (positions |> Array.map Vec.normalize)
            Tangents    = None
            TexCoords   = [ texCoord0, Set.empty; texCoord1, Set.ofList [TextureSemantic.BaseColor] ]
            Colors      = Some colors
        }

    let image =
        {
            Name      = Some "tiny"
            Data      = TestData.tinyPng
            MimeType  = Some "image/png"
            Semantics = Set.ofList [TextureSemantic.BaseColor]
        }

    {
        Materials = Map.ofList [ materialId, material ]
        Meshes = Map.ofList [ meshId, mesh ]
        ImageData = Map.ofList [ imageId, image ]
        RootNode =
            {
                Name = Some "root"
                Trafo = None
                Meshes = [ { Mesh = meshId; Material = Some materialId } ]
                Children = []
            }
    }

let private assertRoundtripScene (scene : Scene) =
    let mesh = expectSingleMesh scene
    let colors = expectColors scene

    CollectionAssert.AreEqual(
        [|
            C4b(255, 0, 0, 255)
            C4b(0, 128, 0, 128)
            C4b(0, 0, 64, 64)
        |],
        colors
    )

    Assert.That(List.length mesh.TexCoords, Is.EqualTo 2)

    let uv0, sem0 = List.item 0 mesh.TexCoords
    let uv1, sem1 = List.item 1 mesh.TexCoords

    CollectionAssert.AreEqual(
        [|
            V2f(0.0f, 0.0f)
            V2f(1.0f, 0.0f)
            V2f(0.0f, 1.0f)
        |],
        uv0
    )

    CollectionAssert.AreEqual(
        [|
            V2f(0.25f, 0.75f)
            V2f(0.75f, 0.75f)
            V2f(0.25f, 0.25f)
        |],
        uv1
    )

    Assert.That(sem0, Is.EqualTo (Set.empty<TextureSemantic>))
    Assert.That(sem1, Is.EqualTo (Set.ofList [TextureSemantic.BaseColor]))

[<Test>]
let ``GLTF.toArray working``() =
    testScene |> GLTF.toArray |> ignore

[<Test>]
let ``GLTF.toString working``() =
    testScene |> GLTF.toString |> ignore

[<Test>]
let ``GLTF.readFrom working with GLB``() =
    use stream = resourceStream "Avocado.glb"
    GLTF.readFrom stream |> ignore

[<Test>]
let ``GLTF.readFrom working with GLTF``() =
    use stream = resourceStream "2CylinderEngine.gltf"
    GLTF.readFrom stream |> ignore

[<Test>]
let ``GLTF.readFrom supports non seekable GLB streams``() =
    let bytes = resourceBytes "Avocado.glb"
    use inner = new MemoryStream(bytes)
    use stream = new NonSeekableStream(inner)

    let scene = GLTF.readFrom stream

    Assert.That(scene.Meshes.Count, Is.GreaterThan 0)
    Assert.That(scene.ImageData.Count, Is.GreaterThan 0)

[<Test>]
let ``GLTF.ofString working``() =
    use stream = resourceStream "2CylinderEngine.gltf"
    use reader = new StreamReader(stream)
    let str = reader.ReadToEnd()
    GLTF.ofString str |> ignore

[<Test>]
let ``GLTF.ofArray working``() =
    use stream = resourceStream "2CylinderEngine.gltf"
    let data = stream.ReadToEnd()
    GLTF.ofArray data |> ignore

[<Test>]
let ``GLTF.ofZipArchive working``() =
    use stream = resourceStream "Avocado.zip"
    use arch = new ZipArchive(stream, ZipArchiveMode.Read)
    GLTF.ofZipArchive arch |> ignore

[<Test>]
let ``GLTF.ofZipArchive resolves nested assets relative to the model entry``() =
    let zip = createNestedZipFixture()
    use stream = new MemoryStream(zip)
    use arch = new ZipArchive(stream, ZipArchiveMode.Read)

    let scene = GLTF.ofZipArchive arch
    let image = expectSingleImage scene

    Assert.That(scene.Meshes.Count, Is.EqualTo 1)
    CollectionAssert.AreEqual(TestData.tinyPng, image.Data)

[<Test>]
let ``GLTF.ofZipArchive resolves parent directory assets relative to the model entry``() =
    let zip = createParentRelativeZipFixture()
    use stream = new MemoryStream(zip)
    use arch = new ZipArchive(stream, ZipArchiveMode.Read)

    let scene = GLTF.ofZipArchive arch
    let image = expectSingleImage scene

    Assert.That(scene.Meshes.Count, Is.EqualTo 1)
    CollectionAssert.AreEqual(TestData.tinyPng, image.Data)

[<Test>]
let ``GLTF.ofZipArchive does not resolve assets outside archive root``() =
    let zip = createEscapingZipFixture()
    use stream = new MemoryStream(zip)
    use arch = new ZipArchive(stream, ZipArchiveMode.Read)

    let scene = GLTF.ofZipArchive arch

    Assert.That(scene.Meshes.Count, Is.EqualTo 1)
    Assert.That(scene.ImageData.Count, Is.EqualTo 0)

[<Test>]
let ``GLTF.ofString preserves unmapped texcoords and sorts them by index``() =
    let scene = createTexCoordFixture() |> GLTF.ofString
    let mesh = expectSingleMesh scene

    Assert.That(List.length mesh.TexCoords, Is.EqualTo 2)

    let uv0, sem0 = List.item 0 mesh.TexCoords
    let uv1, sem1 = List.item 1 mesh.TexCoords

    CollectionAssert.AreEqual(
        [|
            V2f(0.0f, 1.0f)
            V2f(1.0f, 1.0f)
            V2f(0.0f, 0.0f)
        |],
        uv0
    )

    CollectionAssert.AreEqual(
        [|
            V2f(0.25f, 0.50f)
            V2f(0.75f, 0.50f)
            V2f(0.25f, 1.00f)
        |],
        uv1
    )

    Assert.That(sem0, Is.EqualTo (Set.empty<TextureSemantic>))
    Assert.That(sem1, Is.EqualTo (Set.ofList [TextureSemantic.BaseColor]))

[<Test>]
let ``GLTF.ofString normalizes VEC3 and VEC4 byte vertex colors``() =
    let vec3 =
        TestData.bytesOfUInt8s
            [|
                255uy; 0uy; 0uy
                0uy; 128uy; 0uy
                0uy; 0uy; 64uy
            |]
        |> createColorFixture 5121 "VEC3" true
        |> GLTF.ofString
        |> expectColors

    let vec4 =
        TestData.bytesOfUInt8s
            [|
                255uy; 0uy; 0uy; 255uy
                0uy; 128uy; 0uy; 128uy
                0uy; 0uy; 64uy; 64uy
            |]
        |> createColorFixture 5121 "VEC4" true
        |> GLTF.ofString
        |> expectColors

    CollectionAssert.AreEqual(
        [|
            C4b(255, 0, 0, 255)
            C4b(0, 128, 0, 255)
            C4b(0, 0, 64, 255)
        |],
        vec3
    )

    CollectionAssert.AreEqual(
        [|
            C4b(255, 0, 0, 255)
            C4b(0, 128, 0, 128)
            C4b(0, 0, 64, 64)
        |],
        vec4
    )

[<Test>]
let ``GLTF.ofString normalizes ushort vertex colors``() =
    let colors =
        TestData.bytesOfUInt16s
            [|
                65535us; 0us; 0us; 65535us
                0us; 32768us; 0us; 32768us
                0us; 0us; 16384us; 65535us
            |]
        |> createColorFixture 5123 "VEC4" true
        |> GLTF.ofString
        |> expectColors

    CollectionAssert.AreEqual(
        [|
            C4b(255, 0, 0, 255)
            C4b(0, 128, 0, 128)
            C4b(0, 0, 64, 255)
        |],
        colors
    )

[<Test>]
let ``GLTF.ofString normalizes float vertex colors``() =
    let vec3 =
        TestData.bytesOfFloat32s
            [|
                1.0f; 0.0f; 0.0f
                0.0f; 0.5f; 0.0f
                0.0f; 0.0f; 0.25f
            |]
        |> createColorFixture 5126 "VEC3" false
        |> GLTF.ofString
        |> expectColors

    let vec4 =
        TestData.bytesOfFloat32s
            [|
                1.0f; 0.0f; 0.0f; 1.0f
                0.0f; 0.5f; 0.0f; 0.5f
                0.0f; 0.0f; 0.25f; 0.25f
            |]
        |> createColorFixture 5126 "VEC4" false
        |> GLTF.ofString
        |> expectColors

    CollectionAssert.AreEqual(
        [|
            C4b(255, 0, 0, 255)
            C4b(0, 128, 0, 255)
            C4b(0, 0, 64, 255)
        |],
        vec3
    )

    CollectionAssert.AreEqual(
        [|
            C4b(255, 0, 0, 255)
            C4b(0, 128, 0, 128)
            C4b(0, 0, 64, 64)
        |],
        vec4
    )

[<Test>]
let ``GLTF.load working``() =
    let file = Path.ChangeExtension(Path.GetTempFileName(), ".gltf")
    do
        use stream = resourceStream "2CylinderEngine.gltf"
        use dst = File.OpenWrite file
        stream.CopyTo dst
    GLTF.load file |> ignore

[<Test>]
let ``GLTF.save working``() =
    let file = Path.ChangeExtension(Path.GetTempFileName(), ".gltf")
    try testScene |> GLTF.save file
    finally
        try File.Delete file with _ -> ()

[<Test>]
let ``GLTF.roundtrip GLB``() =
    testScene |> GLTF.toArray |> GLTF.ofArray |> ignore

[<Test>]
let ``GLTF.roundtrip GLTF``() =
    testScene |> GLTF.toString |> GLTF.ofString |> ignore

[<Test>]
let ``GLTF.roundtrip GLB preserves colors and texcoords``() =
    let scene = createRoundtripScene() |> GLTF.toArray |> GLTF.ofArray
    assertRoundtripScene scene

[<Test>]
let ``GLTF.roundtrip GLTF preserves colors and texcoords``() =
    let scene = createRoundtripScene() |> GLTF.toString |> GLTF.ofString
    assertRoundtripScene scene
