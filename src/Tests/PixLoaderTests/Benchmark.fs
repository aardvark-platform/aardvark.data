namespace Aardvark.Data.Tests.PixLoader

open System.IO
open Aardvark.Base
open Aardvark.Data
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Running
open BenchmarkDotNet.Filters

type BenchmarkConfig() as this =
    inherit ManualConfig()
    do
        this.AddFilter(
            { new IFilter with
                member _.Predicate(benchmarkCase: BenchmarkCase) =
                    let method = benchmarkCase.Descriptor.WorkloadMethod.Name
                    let format = benchmarkCase.Parameters.Items |> Seq.find (_.Name >> (=) "Format") |> _.Value |> unbox<PixFileFormat>
                    not (method = "DevIL" && format = PixFileFormat.Tiff) &&
                    not (method = "ImageSharp" && format = PixFileFormat.Tiff) &&
                    not (method = "DevIL" && format = PixFileFormat.Exr) &&
                    not (method = "ImageSharp" && format = PixFileFormat.Exr)
            }
        ) |> ignore

type PixImageBenchmark() =
    [<DefaultValue; Params(256, 1024, 4096)>]
    val mutable Size : int

    [<DefaultValue; Params(PixFileFormat.Png, PixFileFormat.Jpeg, PixFileFormat.Tiff, PixFileFormat.Exr)>]
    val mutable Format : PixFileFormat

    member val Image = Unchecked.defaultof<PixImage> with get, set
    member val Stream = null with get, set

    [<GlobalSetup>]
    member this.Setup() =
        Aardvark.Init()

        this.Stream <- new MemoryStream()

        if this.Format = PixFileFormat.Exr then
            this.Image <- PixImage.random typeof<float32> Col.Format.RGBA <| V2i(this.Size)
        else
            this.Image <- PixImage.random typeof<byte> Col.Format.RGBA <| V2i(this.Size)

        this.Image.Save(this.Stream, this.Format, PixCodec.Loader)

    [<GlobalCleanup>]
    member this.Cleanup() =
        if notNull this.Stream then
            this.Stream.Dispose()
            this.Stream <- null


// BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.9711)
// Unknown processor
// .NET SDK 10.0.403
//   [Host] : .NET 10.0.14 (10.0.1426.52326), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI DEBUG
//
// Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3
// LaunchCount=1  WarmupCount=3
//
// | Method     | Size | Format | Mean           | Error           | StdDev       | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated   | Alloc Ratio |
// |----------- |----- |------- |---------------:|----------------:|-------------:|------:|--------:|---------:|---------:|---------:|------------:|------------:|
// | DevIL      | 256  | Jpeg   |     3,982.5 us |       275.91 us |     15.12 us |  2.59 |    0.05 |        - |        - |        - |    120713 B |        5.72 |
// | FreeImage  | 256  | Jpeg   |       417.6 us |        97.80 us |      5.36 us |  0.27 |    0.00 |        - |        - |        - |    198435 B |        9.40 |
// | ImageSharp | 256  | Jpeg   |       720.3 us |       179.47 us |      9.84 us |  0.47 |    0.01 |   1.9531 |        - |        - |    136630 B |        6.47 |
// | PixCodec   | 256  | Jpeg   |     1,537.1 us |       399.95 us |     21.92 us |  1.00 |    0.00 |   3.9063 |   3.9063 |   3.9063 |     21110 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | DevIL      | 256  | Png    |    16,995.3 us |     6,467.99 us |    354.53 us | 37.89 |    0.77 |        - |        - |        - |    264846 B |       29.65 |
// | FreeImage  | 256  | Png    |     5,418.3 us |     2,208.29 us |    121.04 us | 12.08 |    0.25 |        - |        - |        - |      1546 B |        0.17 |
// | ImageSharp | 256  | Png    |     4,702.0 us |     1,951.57 us |    106.97 us | 10.48 |    0.22 |        - |        - |        - |    933780 B |      104.55 |
// | PixCodec   | 256  | Png    |       448.5 us |        12.14 us |      0.67 us |  1.00 |    0.00 |        - |        - |        - |      8931 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | FreeImage  | 256  | Tiff   |     1,652.5 us |       105.30 us |      5.77 us | 11.84 |    0.09 |        - |        - |        - |      1508 B |        0.20 |
// | PixCodec   | 256  | Tiff   |       139.6 us |        19.26 us |      1.06 us |  1.00 |    0.00 |        - |        - |        - |      7726 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | FreeImage  | 256  | Exr    |    30,822.3 us |     2,489.58 us |    136.46 us | 42.46 |    0.32 |        - |        - |        - |      1694 B |       0.001 |
// | PixCodec   | 256  | Exr    |       726.0 us |        92.07 us |      5.05 us |  1.00 |    0.00 |  43.9453 |  21.4844 |        - |   2122583 B |       1.000 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | DevIL      | 1024 | Jpeg   |    63,136.5 us |     5,329.85 us |    292.15 us |  2.63 |    0.01 |        - |        - |        - |   1896962 B |       88.22 |
// | FreeImage  | 1024 | Jpeg   |     7,288.4 us |       234.60 us |     12.86 us |  0.30 |    0.00 |  62.5000 |  62.5000 |  62.5000 |   3148497 B |      146.43 |
// | ImageSharp | 1024 | Jpeg   |    10,816.7 us |       838.58 us |     45.97 us |  0.45 |    0.00 |        - |        - |        - |    493075 B |       22.93 |
// | PixCodec   | 1024 | Jpeg   |    24,028.0 us |     1,386.98 us |     76.03 us |  1.00 |    0.00 |        - |        - |        - |     21502 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | DevIL      | 1024 | Png    |   278,553.2 us |    23,663.66 us |  1,297.08 us | 30.25 |    0.04 |        - |        - |        - |   4207712 B |      137.82 |
// | FreeImage  | 1024 | Png    |    87,148.0 us |    11,344.45 us |    621.83 us |  9.46 |    0.02 |        - |        - |        - |      2555 B |        0.08 |
// | ImageSharp | 1024 | Png    |    76,300.1 us |     1,710.43 us |     93.75 us |  8.28 |    0.05 |        - |        - |        - |  13395571 B |      438.75 |
// | PixCodec   | 1024 | Png    |     9,209.9 us |       949.10 us |     52.02 us |  1.00 |    0.00 |        - |        - |        - |     30531 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | FreeImage  | 1024 | Tiff   |    26,642.6 us |     1,736.13 us |     95.16 us | 19.03 |    0.16 |        - |        - |        - |      1694 B |        0.10 |
// | PixCodec   | 1024 | Tiff   |     1,399.9 us |       120.94 us |      6.63 us |  1.00 |    0.00 |        - |        - |        - |     16991 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | FreeImage  | 1024 | Exr    |   319,739.7 us |    76,268.60 us |  4,180.54 us | 47.72 |    0.73 |        - |        - |        - |      4672 B |       0.000 |
// | PixCodec   | 1024 | Exr    |     6,700.4 us |       549.78 us |     30.14 us |  1.00 |    0.00 | 312.5000 | 312.5000 | 312.5000 |  33609156 B |       1.000 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | DevIL      | 4096 | Jpeg   | 1,026,218.6 us |    25,655.59 us |  1,406.27 us |  2.65 |    0.01 |        - |        - |        - |  30322040 B |    1,079.54 |
// | FreeImage  | 4096 | Jpeg   |   122,241.6 us |     8,364.65 us |    458.49 us |  0.32 |    0.00 |        - |        - |        - |  50334742 B |    1,792.04 |
// | ImageSharp | 4096 | Jpeg   |   175,395.5 us |     7,550.09 us |    413.85 us |  0.45 |    0.00 |        - |        - |        - |   1922101 B |       68.43 |
// | PixCodec   | 4096 | Jpeg   |   387,753.0 us |    23,335.20 us |  1,279.08 us |  1.00 |    0.00 |        - |        - |        - |     28088 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | DevIL      | 4096 | Png    | 4,464,471.7 us |   376,640.24 us | 20,644.92 us | 33.51 |    0.18 |        - |        - |        - |  67239912 B |      569.66 |
// | FreeImage  | 4096 | Png    | 1,398,262.3 us |    43,071.73 us |  2,360.91 us | 10.49 |    0.01 |        - |        - |        - |           - |        0.00 |
// | ImageSharp | 4096 | Png    | 1,218,563.2 us |    24,728.36 us |  1,355.45 us |  9.15 |    0.01 |        - |        - |        - | 208508120 B |    1,766.48 |
// | PixCodec   | 4096 | Png    |   133,237.9 us |     1,722.13 us |     94.40 us |  1.00 |    0.00 |        - |        - |        - |    118036 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | FreeImage  | 4096 | Tiff   |   423,921.3 us |    51,171.00 us |  2,804.86 us | 18.81 |    0.11 |        - |        - |        - |           - |        0.00 |
// | PixCodec   | 4096 | Tiff   |    22,539.8 us |       840.54 us |     46.07 us |  1.00 |    0.00 |        - |        - |        - |    145054 B |        1.00 |
// |            |      |        |                |                 |              |       |         |          |          |          |             |             |
// | FreeImage  | 4096 | Exr    | 2,464,045.2 us | 1,344,704.50 us | 73,707.79 us | 24.95 |    0.51 |        - |        - |        - |           - |        0.00 |
// | PixCodec   | 4096 | Exr    |    98,755.6 us |    17,320.86 us |    949.41 us |  1.00 |    0.00 | 400.0000 | 400.0000 | 400.0000 | 537859523 B |        1.00 |

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


// BenchmarkDotNet v0.13.12, Windows 11 (10.0.26100.9711)
// Unknown processor
// .NET SDK 10.0.403
//   [Host] : .NET 10.0.14 (10.0.1426.52326), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI DEBUG
//
// Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3
// LaunchCount=1  WarmupCount=3
//
// | Method     | Size | Format | Mean          | Error         | StdDev       | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated    | Alloc Ratio |
// |----------- |----- |------- |--------------:|--------------:|-------------:|------:|--------:|---------:|---------:|---------:|-------------:|------------:|
// | DevIL      | 256  | Jpeg   |   1,607.30 us |    481.210 us |    26.377 us |  2.53 |    0.03 |  99.6094 |  99.6094 |  99.6094 |    310.56 KB |        1.58 |
// | FreeImage  | 256  | Jpeg   |     938.63 us |    341.382 us |    18.712 us |  1.48 |    0.05 |  61.5234 |  61.5234 |  61.5234 |    194.09 KB |        0.98 |
// | ImageSharp | 256  | Jpeg   |   1,111.27 us |    453.432 us |    24.854 us |  1.75 |    0.06 |  60.5469 |  58.5938 |  58.5938 |    325.51 KB |        1.65 |
// | PixCodec   | 256  | Jpeg   |     636.11 us |    150.526 us |     8.251 us |  1.00 |    0.00 |  62.5000 |  62.5000 |  62.5000 |    197.06 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | DevIL      | 256  | Png    |     539.41 us |    172.653 us |     9.464 us |  7.43 |    0.12 | 127.4414 | 127.4414 | 127.4414 |    515.46 KB |        1.13 |
// | FreeImage  | 256  | Png    |     369.15 us |    149.329 us |     8.185 us |  5.09 |    0.11 |  74.7070 |  74.7070 |  74.7070 |     258.6 KB |        0.57 |
// | ImageSharp | 256  | Png    |     266.05 us |    232.358 us |    12.736 us |  3.67 |    0.18 |  71.2891 |  68.8477 |  68.8477 |    378.98 KB |        0.83 |
// | PixCodec   | 256  | Png    |      72.56 us |      1.936 us |     0.106 us |  1.00 |    0.00 |  79.7119 |  78.9795 |  78.9795 |    455.58 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | FreeImage  | 256  | Tiff   |     389.06 us |     79.040 us |     4.332 us |  8.34 |    0.10 |  68.3594 |  68.3594 |  68.3594 |    258.54 KB |        0.98 |
// | PixCodec   | 256  | Tiff   |      46.67 us |      5.358 us |     0.294 us |  1.00 |    0.00 |  46.3257 |  46.2036 |  46.2036 |    263.25 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | FreeImage  | 256  | Exr    |     814.81 us |    195.019 us |    10.690 us |  7.76 |    0.19 |  90.8203 |  90.8203 |  90.8203 |   1026.76 KB |        0.99 |
// | PixCodec   | 256  | Exr    |     104.99 us |     30.379 us |     1.665 us |  1.00 |    0.00 |  64.6973 |  64.4531 |  64.4531 |   1041.12 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | DevIL      | 1024 | Jpeg   |  23,669.05 us |  3,851.097 us |   211.092 us |  2.34 |    0.03 | 125.0000 | 125.0000 | 125.0000 |   4934.06 KB |        1.60 |
// | FreeImage  | 1024 | Jpeg   |  14,756.84 us |  5,292.105 us |   290.078 us |  1.46 |    0.05 |  78.1250 |  78.1250 |  78.1250 |   3074.91 KB |        1.00 |
// | ImageSharp | 1024 | Jpeg   |  16,923.56 us |  8,532.387 us |   467.689 us |  1.67 |    0.07 |  93.7500 |  93.7500 |  93.7500 |   3553.71 KB |        1.15 |
// | PixCodec   | 1024 | Jpeg   |  10,125.09 us |  2,366.480 us |   129.715 us |  1.00 |    0.00 | 500.0000 | 500.0000 | 500.0000 |   3079.38 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | DevIL      | 1024 | Png    |   9,032.45 us |  2,149.676 us |   117.831 us |  6.86 |    0.16 | 312.5000 | 312.5000 | 312.5000 |    8197.7 KB |        1.91 |
// | FreeImage  | 1024 | Png    |   5,474.85 us |  1,571.042 us |    86.114 us |  4.16 |    0.02 | 179.6875 | 179.6875 | 179.6875 |   4100.12 KB |        0.95 |
// | ImageSharp | 1024 | Png    |   3,064.34 us |  1,114.893 us |    61.111 us |  2.33 |    0.07 | 199.2188 | 191.4063 | 191.4063 |   4569.26 KB |        1.06 |
// | PixCodec   | 1024 | Png    |   1,317.42 us |    278.892 us |    15.287 us |  1.00 |    0.00 | 244.1406 | 244.1406 | 244.1406 |   4296.18 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | FreeImage  | 1024 | Tiff   |   4,726.80 us |    345.733 us |    18.951 us |  7.26 |    0.54 | 140.6250 | 140.6250 | 140.6250 |   4099.56 KB |        0.99 |
// | PixCodec   | 1024 | Tiff   |     653.49 us |    813.135 us |    44.571 us |  1.00 |    0.00 | 124.0234 | 123.0469 | 123.0469 |   4127.11 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | FreeImage  | 1024 | Exr    |  11,464.09 us |  3,363.539 us |   184.367 us | 15.48 |    0.69 | 140.6250 | 140.6250 | 140.6250 |   16387.4 KB |        1.00 |
// | PixCodec   | 1024 | Exr    |     741.47 us |    544.961 us |    29.871 us |  1.00 |    0.00 |  33.2031 |  33.2031 |  33.2031 |  16408.02 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | DevIL      | 4096 | Jpeg   | 375,246.30 us | 38,538.223 us | 2,112.410 us |  2.44 |    0.01 |        - |        - |        - |  78909.41 KB |        1.61 |
// | FreeImage  | 4096 | Jpeg   | 229,177.12 us | 43,737.829 us | 2,397.418 us |  1.49 |    0.02 |        - |        - |        - |  49155.54 KB |        1.00 |
// | ImageSharp | 4096 | Jpeg   | 265,294.93 us | 25,857.852 us | 1,417.356 us |  1.73 |    0.01 |        - |        - |        - |  51029.05 KB |        1.04 |
// | PixCodec   | 4096 | Jpeg   | 153,487.77 us |    968.203 us |    53.070 us |  1.00 |    0.00 | 250.0000 | 250.0000 | 250.0000 |  49159.33 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | DevIL      | 4096 | Png    | 137,704.02 us | 34,321.905 us | 1,881.299 us |  7.79 |    0.06 |        - |        - |        - | 131088.36 KB |        1.99 |
// | FreeImage  | 4096 | Png    |  85,103.38 us | 16,064.360 us |   880.542 us |  4.81 |    0.08 |        - |        - |        - |  65538.67 KB |        1.00 |
// | ImageSharp | 4096 | Png    |  48,257.30 us |  2,890.380 us |   158.432 us |  2.73 |    0.05 |        - |        - |        - |  67424.47 KB |        1.03 |
// | PixCodec   | 4096 | Png    |  17,686.88 us |  5,838.619 us |   320.034 us |  1.00 |    0.00 |  93.7500 |  93.7500 |  93.7500 |  65735.55 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | FreeImage  | 4096 | Tiff   |  73,946.00 us | 12,799.750 us |   701.597 us | 19.93 |    0.51 |        - |        - |        - |  65538.55 KB |        1.00 |
// | PixCodec   | 4096 | Tiff   |   3,710.61 us |  1,101.169 us |    60.359 us |  1.00 |    0.00 |  82.0313 |  82.0313 |  82.0313 |  65598.05 KB |        1.00 |
// |            |      |        |               |               |              |       |         |          |          |          |              |             |
// | FreeImage  | 4096 | Exr    | 181,588.58 us | 47,521.634 us | 2,604.821 us | 12.96 |    0.30 |        - |        - |        - | 262147.54 KB |        1.00 |
// | PixCodec   | 4096 | Exr    |  14,011.80 us |  2,892.950 us |   158.572 us |  1.00 |    0.00 | 156.2500 | 156.2500 | 156.2500 | 262196.51 KB |        1.00 |

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