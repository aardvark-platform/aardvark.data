namespace Aardvark.Data.Tests.PixLoader

open System.IO
open Aardvark.Base
open Aardvark.Data
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open BenchmarkDotNet.Filters

open PixLoaderTests

type BenchmarkConfig() as this =
    inherit ManualConfig()
    do
        this.AddFilter(
            { new IFilter with
                member _.Predicate(benchmarkCase: BenchmarkCase) =
                    let method = benchmarkCase.Descriptor.WorkloadMethod.Name
                    let format = benchmarkCase.Parameters.Items |> Seq.find (_.Name >> (=) "Format") |> _.Value |> unbox<PixFileFormat>
                    not (method = "DevIL" && format = PixFileFormat.Tiff) &&
                    not (method = "ImageSharp" && format = PixFileFormat.Tiff)
            }
        ) |> ignore

type PixImageBenchmark() =
    [<DefaultValue; Params(256, 1024, 4096)>]
    val mutable Size : int

    [<DefaultValue; Params(PixFileFormat.Png, PixFileFormat.Jpeg, PixFileFormat.Tiff)>]
    val mutable Format : PixFileFormat

    member val Image = Unchecked.defaultof<PixImage<byte>> with get, set
    member val Stream = null with get, set

    [<GlobalSetup>]
    member this.Setup() =
        Aardvark.Init()

        this.Stream <- new MemoryStream()
        this.Image <- PixImage.checkerboard Col.Format.RGBA this.Size this.Size
        this.Image.Save(this.Stream, this.Format, PixCodec.Loader)

    [<GlobalCleanup>]
    member this.Cleanup() =
        if notNull this.Stream then
            this.Stream.Dispose()
            this.Stream <- null


// BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9711)
// Unknown processor
// .NET SDK 8.0.424
//   [Host] : .NET 8.0.31 (8.0.3126.42015), X64 RyuJIT AVX2 DEBUG
//
// Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3
// LaunchCount=1  WarmupCount=3
//
// | Method     | Size | Format | Mean           | Error           | StdDev        | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated    | Alloc Ratio |
// |----------- |----- |------- |---------------:|----------------:|--------------:|------:|--------:|---------:|---------:|---------:|-------------:|------------:|
// | DevIL      | 256  | Jpeg   |     3,271.2 us |        69.61 us |       3.82 us |  3.75 |    0.03 |   3.9063 |        - |        - |     70.29 KB |        0.08 |
// | FreeImage  | 256  | Jpeg   |       627.8 us |        64.02 us |       3.51 us |  0.72 |    0.00 |  30.2734 |  30.2734 |  30.2734 |    194.05 KB |        0.21 |
// | ImageSharp | 256  | Jpeg   |       518.1 us |        24.39 us |       1.34 us |  0.59 |    0.00 |   7.8125 |        - |        - |    133.55 KB |        0.15 |
// | PixCodec   | 256  | Jpeg   |       873.0 us |        97.84 us |       5.36 us |  1.00 |    0.00 | 210.9375 | 203.1250 | 199.2188 |    918.44 KB |        1.00 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | DevIL      | 256  | Png    |     4,373.0 us |       122.19 us |       6.70 us |  6.45 |    0.08 |        - |        - |        - |      6.09 KB |       0.006 |
// | FreeImage  | 256  | Png    |     2,399.3 us |        43.11 us |       2.36 us |  3.54 |    0.05 |        - |        - |        - |      1.47 KB |       0.002 |
// | ImageSharp | 256  | Png    |     1,427.9 us |       197.64 us |      10.83 us |  2.10 |    0.01 |   7.8125 |        - |        - |    143.74 KB |       0.148 |
// | PixCodec   | 256  | Png    |       678.6 us |       174.26 us |       9.55 us |  1.00 |    0.00 | 255.8594 | 250.9766 | 248.0469 |     972.6 KB |       1.000 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | FreeImage  | 256  | Tiff   |     1,143.1 us |        41.51 us |       2.28 us |  4.88 |    0.04 |        - |        - |        - |      1.46 KB |        0.11 |
// | PixCodec   | 256  | Tiff   |       234.4 us |        23.98 us |       1.31 us |  1.00 |    0.00 |   0.7324 |        - |        - |     13.82 KB |        1.00 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | DevIL      | 1024 | Jpeg   |    51,850.8 us |       493.21 us |      27.03 us |  5.06 |    0.01 |        - |        - |        - |    1063.3 KB |        0.11 |
// | FreeImage  | 1024 | Jpeg   |    10,031.1 us |       602.08 us |      33.00 us |  0.98 |    0.00 | 218.7500 | 218.7500 | 218.7500 |   3075.71 KB |        0.32 |
// | ImageSharp | 1024 | Jpeg   |     7,599.7 us |       287.98 us |      15.79 us |  0.74 |    0.00 |  23.4375 |        - |        - |    481.56 KB |        0.05 |
// | PixCodec   | 1024 | Jpeg   |    10,242.7 us |       293.42 us |      16.08 us |  1.00 |    0.00 | 484.3750 | 484.3750 | 484.3750 |   9563.25 KB |        1.00 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | DevIL      | 1024 | Png    |    71,004.0 us |       685.99 us |      37.60 us |  6.63 |    0.01 |        - |        - |        - |     60.69 KB |       0.061 |
// | FreeImage  | 1024 | Png    |    38,896.1 us |     1,529.78 us |      83.85 us |  3.63 |    0.01 |        - |        - |        - |      1.56 KB |       0.002 |
// | ImageSharp | 1024 | Png    |    21,966.7 us |       668.63 us |      36.65 us |  2.05 |    0.01 |  31.2500 |        - |        - |    661.91 KB |       0.666 |
// | PixCodec   | 1024 | Png    |    10,701.9 us |       459.67 us |      25.20 us |  1.00 |    0.00 |  93.7500 |  93.7500 |  93.7500 |    993.42 KB |       1.000 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | FreeImage  | 1024 | Tiff   |    17,920.0 us |       599.26 us |      32.85 us |  7.25 |    0.06 |        - |        - |        - |       1.5 KB |        0.01 |
// | PixCodec   | 1024 | Tiff   |     2,473.3 us |       305.26 us |      16.73 us |  1.00 |    0.00 |   7.8125 |        - |        - |    142.01 KB |        1.00 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | DevIL      | 4096 | Jpeg   |   815,057.3 us |    28,171.44 us |   1,544.17 us |  5.07 |    0.01 |        - |        - |        - |  16894.27 KB |        0.11 |
// | FreeImage  | 4096 | Jpeg   |   157,919.2 us |     5,556.91 us |     304.59 us |  0.98 |    0.00 |        - |        - |        - |   49154.1 KB |        0.33 |
// | ImageSharp | 4096 | Jpeg   |   129,491.6 us |    15,392.55 us |     843.72 us |  0.80 |    0.00 |        - |        - |        - |   1875.37 KB |        0.01 |
// | PixCodec   | 4096 | Jpeg   |   160,917.8 us |     6,579.95 us |     360.67 us |  1.00 |    0.00 | 500.0000 | 500.0000 | 500.0000 | 147815.65 KB |        1.00 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | DevIL      | 4096 | Png    | 1,126,135.3 us |    48,898.36 us |   2,680.28 us |  7.66 |    0.19 |        - |        - |        - |     909.6 KB |       0.844 |
// | FreeImage  | 4096 | Png    |   727,322.6 us | 3,715,428.71 us | 203,655.18 us |  4.96 |    1.48 |        - |        - |        - |      2.74 KB |       0.003 |
// | ImageSharp | 4096 | Png    |   334,662.2 us |    13,133.50 us |     719.89 us |  2.28 |    0.06 |        - |        - |        - |   4882.61 KB |       4.532 |
// | PixCodec   | 4096 | Png    |   147,001.7 us |    67,219.27 us |   3,684.52 us |  1.00 |    0.00 |        - |        - |        - |   1077.38 KB |       1.000 |
// |            |      |        |                |                 |               |       |         |          |          |          |              |             |
// | FreeImage  | 4096 | Tiff   |   285,028.2 us |    11,914.02 us |     653.05 us |  7.06 |    0.26 |        - |        - |        - |       2.1 KB |       0.001 |
// | PixCodec   | 4096 | Tiff   |    40,405.8 us |    26,892.20 us |   1,474.05 us |  1.00 |    0.00 | 230.7692 |  76.9231 |        - |   3507.73 KB |       1.000 |

[<Config(typeof<BenchmarkConfig>)>]
[<MemoryDiagnoser>]
type PixImageSaveBenchmark() =
    inherit PixImageBenchmark()

    member inline private this.Save(loader) =
        this.Stream.Position <- 0L
        this.Stream.SetLength 0L
        this.Image.Save(this.Stream, this.Format, loader)

    [<Benchmark>] member this.DevIL()                     = this.Save PixImageDevil.Loader
    [<Benchmark>] member this.FreeImage()                 = this.Save PixImageFreeImage.Loader
    [<Benchmark>] member this.ImageSharp()                = this.Save PixImageSharp.Loader
    [<Benchmark(Baseline = true)>] member this.PixCodec() = this.Save PixCodec.Loader


// BenchmarkDotNet v0.13.12, Windows 11 (10.0.26300.9711)
// Unknown processor
// .NET SDK 8.0.424
//   [Host] : .NET 8.0.31 (8.0.3126.42015), X64 RyuJIT AVX2 DEBUG
//
// Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3
// LaunchCount=1  WarmupCount=3
//
// | Method     | Size | Format | Mean         | Error        | StdDev      | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated    | Alloc Ratio |
// |----------- |----- |------- |-------------:|-------------:|------------:|------:|--------:|---------:|---------:|---------:|-------------:|------------:|
// | DevIL      | 256  | Jpeg   |   1,006.8 us |  1,110.66 us |    60.88 us |  2.39 |    0.15 |  61.5234 |  61.5234 |  61.5234 |    243.62 KB |        0.59 |
// | FreeImage  | 256  | Jpeg   |     661.5 us |     66.52 us |     3.65 us |  1.57 |    0.01 |  61.5234 |  61.5234 |  61.5234 |    193.86 KB |        0.47 |
// | ImageSharp | 256  | Jpeg   |     778.6 us |     16.26 us |     0.89 us |  1.85 |    0.01 |  61.5234 |  61.5234 |  61.5234 |    325.37 KB |        0.79 |
// | PixCodec   | 256  | Jpeg   |     420.5 us |     29.65 us |     1.63 us |  1.00 |    0.00 |  68.8477 |  68.8477 |  68.8477 |    412.65 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | DevIL      | 256  | Png    |     524.5 us |     39.92 us |     2.19 us |  2.92 |    0.03 |  83.0078 |  83.0078 |  83.0078 |    262.24 KB |        0.57 |
// | FreeImage  | 256  | Png    |     708.9 us |     17.12 us |     0.94 us |  3.94 |    0.04 |  83.0078 |  83.0078 |  83.0078 |    257.91 KB |        0.56 |
// | ImageSharp | 256  | Png    |     985.2 us |    168.29 us |     9.22 us |  5.48 |    0.04 |  82.0313 |  82.0313 |  82.0313 |     378.6 KB |        0.82 |
// | PixCodec   | 256  | Png    |     179.9 us |     32.28 us |     1.77 us |  1.00 |    0.00 | 133.3008 | 133.3008 | 133.3008 |    459.45 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | FreeImage  | 256  | Tiff   |     570.1 us |     95.98 us |     5.26 us |  5.63 |    0.07 |  74.2188 |  74.2188 |  74.2188 |    258.36 KB |        0.96 |
// | PixCodec   | 256  | Tiff   |     101.3 us |      6.79 us |     0.37 us |  1.00 |    0.00 |  57.3730 |  56.5186 |  56.5186 |    269.47 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | DevIL      | 1024 | Jpeg   |  13,150.9 us |    101.60 us |     5.57 us |  2.11 |    0.01 | 296.8750 | 296.8750 | 296.8750 |   3837.32 KB |        0.55 |
// | FreeImage  | 1024 | Jpeg   |   9,879.0 us |    860.74 us |    47.18 us |  1.58 |    0.01 | 156.2500 | 156.2500 | 156.2500 |   3074.61 KB |        0.44 |
// | ImageSharp | 1024 | Jpeg   |  10,590.0 us |    373.45 us |    20.47 us |  1.70 |    0.00 | 265.6250 | 250.0000 | 250.0000 |   3554.09 KB |        0.51 |
// | PixCodec   | 1024 | Jpeg   |   6,236.2 us |    326.65 us |    17.90 us |  1.00 |    0.00 | 976.5625 | 976.5625 | 976.5625 |   6942.87 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | DevIL      | 1024 | Png    |   8,226.6 us |    427.70 us |    23.44 us |  6.85 |    0.81 | 312.5000 | 312.5000 | 312.5000 |   4158.79 KB |        0.95 |
// | FreeImage  | 1024 | Png    |  11,476.2 us |    350.04 us |    19.19 us |  9.55 |    1.14 | 312.5000 | 312.5000 | 312.5000 |   4100.51 KB |        0.94 |
// | ImageSharp | 1024 | Png    |   9,529.9 us |    214.50 us |    11.76 us |  7.93 |    0.93 | 328.1250 | 312.5000 | 312.5000 |   4568.08 KB |        1.05 |
// | PixCodec   | 1024 | Png    |   1,213.3 us |  2,777.85 us |   152.26 us |  1.00 |    0.00 | 343.7500 | 339.8438 | 337.8906 |   4354.81 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | FreeImage  | 1024 | Tiff   |   7,598.2 us |    715.71 us |    39.23 us | 14.65 |    0.34 | 125.0000 | 125.0000 | 125.0000 |   4098.46 KB |        0.97 |
// | PixCodec   | 1024 | Tiff   |     518.8 us |    172.46 us |     9.45 us |  1.00 |    0.00 | 185.5469 | 183.5938 | 183.5938 |   4239.23 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | DevIL      | 4096 | Jpeg   | 205,936.7 us | 16,535.43 us |   906.36 us |  2.43 |    0.01 |        - |        - |        - |  61290.25 KB |        0.55 |
// | FreeImage  | 4096 | Jpeg   | 152,622.9 us |  5,266.53 us |   288.68 us |  1.80 |    0.01 |        - |        - |        - |  49154.13 KB |        0.44 |
// | ImageSharp | 4096 | Jpeg   | 161,385.4 us |  3,776.12 us |   206.98 us |  1.91 |    0.00 |        - |        - |        - |  51026.87 KB |        0.46 |
// | PixCodec   | 4096 | Jpeg   |  84,677.6 us |  1,643.87 us |    90.11 us |  1.00 |    0.00 | 500.0000 | 500.0000 | 500.0000 | 110522.51 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | DevIL      | 4096 | Png    | 133,055.7 us |  6,685.59 us |   366.46 us |  8.15 |    0.04 |        - |        - |        - |  66456.54 KB |        1.00 |
// | FreeImage  | 4096 | Png    | 177,244.3 us | 21,948.83 us | 1,203.09 us | 10.86 |    0.12 |        - |        - |        - |  65538.23 KB |        0.98 |
// | ImageSharp | 4096 | Png    | 128,511.5 us |  7,348.73 us |   402.81 us |  7.88 |    0.06 |        - |        - |        - |   67400.9 KB |        1.01 |
// | PixCodec   | 4096 | Png    |  16,318.1 us |  2,052.95 us |   112.53 us |  1.00 |    0.00 | 468.7500 | 468.7500 | 468.7500 |  66653.62 KB |        1.00 |
// |            |      |        |              |              |             |       |         |          |          |          |              |             |
// | FreeImage  | 4096 | Tiff   | 119,485.3 us | 13,265.54 us |   727.13 us | 16.14 |    0.25 |        - |        - |        - |  65538.06 KB |        0.95 |
// | PixCodec   | 4096 | Tiff   |   7,401.7 us |  1,371.04 us |    75.15 us |  1.00 |    0.00 | 328.1250 | 328.1250 | 328.1250 |  68913.59 KB |        1.00 |

[<Config(typeof<BenchmarkConfig>)>]
[<MemoryDiagnoser>]
type PixImageLoadBenchmark() =
    inherit PixImageBenchmark()

    member inline private this.Load(loader) =
        this.Stream.Position <- 0L
        PixImage.Load(this.Stream, loader)

    [<Benchmark>] member this.DevIL()                     = this.Load PixImageDevil.Loader
    [<Benchmark>] member this.FreeImage()                 = this.Load PixImageFreeImage.Loader
    [<Benchmark>] member this.ImageSharp()                = this.Load PixImageSharp.Loader
    [<Benchmark(Baseline = true)>] member this.PixCodec() = this.Load PixCodec.Loader