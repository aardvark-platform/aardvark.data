namespace Tests

open System.IO
open Aardvark.Base
open BenchmarkDotNet.Attributes;

open PixLoaderTests

[<MemoryDiagnoser>]
type LoaderBenchmark() =
    let mutable pix = Unchecked.defaultof<PixImage<byte>>
    let mutable filename = ""

    [<DefaultValue; Params(256, 1024, 2048, 4096)>]
    val mutable Size : int

    [<DefaultValue; Params(PixFileFormat.Png, PixFileFormat.Jpeg, PixFileFormat.Tiff)>]
    val mutable Format : PixFileFormat

    [<GlobalSetup>]
    member x.Setup() =
        Aardvark.Init()

        filename <- Path.GetTempFileName()
        pix <- PixImage.checkerboard true Col.Format.RGBA x.Size x.Size

    [<GlobalCleanup>]
    member x.Cleanup() =
        if File.Exists filename then
            File.Delete filename

    [<IterationSetup>]
    member x.SetupIteration() =
        pix.Save(filename, x.Format, false)

    member inline private x.Load(loader) =
        PixImage.Load(filename, loader) |> ignore

    [<Benchmark(Description = "Load (DevIL)")>]      member x.Load_DevIL()      = x.Load(PixImageDevil.Loader)
    [<Benchmark(Description = "Load (FreeImage)")>]  member x.Load_FreeImage()  = x.Load(PixImageFreeImage.Loader)
    [<Benchmark(Description = "Load (ImageSharp)")>] member x.Load_ImageSharp() = x.Load(PixImageSharp.Loader)

    member inline private x.Save(loader) =
        pix.Save(filename, x.Format, normalizeFilename = false, loader = loader)

    [<Benchmark(Description = "Save (DevIL)")>]      member x.Save_DevIL()      = x.Save(PixImageDevil.Loader)
    [<Benchmark(Description = "Save (FreeImage)")>]  member x.Save_FreeImage()  = x.Save(PixImageFreeImage.Loader)
    [<Benchmark(Description = "Save (ImageSharp)")>] member x.Save_ImageSharp() = x.Save(PixImageSharp.Loader)