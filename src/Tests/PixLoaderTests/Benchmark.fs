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
// .NET SDK 10.0.401
//   [Host] : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2 DEBUG
//
// Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3
// LaunchCount=1  WarmupCount=3
//
// | Method     | Size | Format | Mean           | Error         | StdDev       | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated   | Alloc Ratio |
// |----------- |----- |------- |---------------:|--------------:|-------------:|------:|--------:|---------:|---------:|---------:|------------:|------------:|
// | DevIL      | 256  | Jpeg   |     3,482.3 us |      84.11 us |      4.61 us |  3.86 |    0.01 |   3.9063 |        - |        - |     71936 B |        0.08 |
// | FreeImage  | 256  | Jpeg   |       597.1 us |      29.04 us |      1.59 us |  0.66 |    0.00 |  27.3438 |  27.3438 |  27.3438 |    198817 B |        0.21 |
// | ImageSharp | 256  | Jpeg   |       487.2 us |      30.63 us |      1.68 us |  0.54 |    0.00 |   7.8125 |        - |        - |    136627 B |        0.15 |
// | PixCodec   | 256  | Jpeg   |       901.2 us |      34.77 us |      1.91 us |  1.00 |    0.00 | 210.9375 | 203.1250 | 199.2188 |    941309 B |        1.00 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | DevIL      | 256  | Png    |     4,438.6 us |     524.93 us |     28.77 us |  7.13 |    0.19 |        - |        - |        - |      6207 B |       0.006 |
// | FreeImage  | 256  | Png    |     2,405.6 us |     131.01 us |      7.18 us |  3.86 |    0.10 |        - |        - |        - |      1520 B |       0.002 |
// | ImageSharp | 256  | Png    |     1,319.1 us |     287.77 us |     15.77 us |  2.12 |    0.04 |   7.8125 |        - |        - |    147196 B |       0.148 |
// | PixCodec   | 256  | Png    |       623.1 us |     280.49 us |     15.37 us |  1.00 |    0.00 | 255.8594 | 250.0000 | 248.0469 |    997117 B |       1.000 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | FreeImage  | 256  | Tiff   |     1,168.4 us |      21.23 us |      1.16 us |  5.09 |    0.06 |        - |        - |        - |      1508 B |        0.11 |
// | PixCodec   | 256  | Tiff   |       229.4 us |      43.70 us |      2.40 us |  1.00 |    0.00 |   0.7324 |        - |        - |     13908 B |        1.00 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | DevIL      | 1024 | Jpeg   |    55,198.1 us |   2,474.36 us |    135.63 us |  5.41 |    0.03 |        - |        - |        - |   1089226 B |        0.11 |
// | FreeImage  | 1024 | Jpeg   |     9,744.7 us |     556.05 us |     30.48 us |  0.95 |    0.01 | 218.7500 | 218.7500 | 218.7500 |   3150716 B |        0.32 |
// | ImageSharp | 1024 | Jpeg   |     7,281.0 us |     672.58 us |     36.87 us |  0.71 |    0.00 |  23.4375 |        - |        - |    493023 B |        0.05 |
// | PixCodec   | 1024 | Jpeg   |    10,211.6 us |   1,062.23 us |     58.22 us |  1.00 |    0.00 | 484.3750 | 484.3750 | 484.3750 |   9794491 B |        1.00 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | DevIL      | 1024 | Png    |    72,112.9 us |   6,388.71 us |    350.19 us |  7.12 |    0.01 |        - |        - |        - |     62779 B |       0.062 |
// | FreeImage  | 1024 | Png    |    39,892.0 us |   5,524.24 us |    302.80 us |  3.94 |    0.03 |        - |        - |        - |      1959 B |       0.002 |
// | ImageSharp | 1024 | Png    |    21,082.6 us |   1,524.01 us |     83.54 us |  2.08 |    0.01 |  31.2500 |        - |        - |    677940 B |       0.666 |
// | PixCodec   | 1024 | Png    |    10,126.3 us |   1,174.18 us |     64.36 us |  1.00 |    0.00 |  93.7500 |  93.7500 |  93.7500 |   1017773 B |       1.000 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | FreeImage  | 1024 | Tiff   |    19,690.5 us |  21,354.95 us |  1,170.54 us |  6.44 |    0.33 |        - |        - |        - |      1684 B |        0.01 |
// | PixCodec   | 1024 | Tiff   |     3,058.2 us |   1,798.13 us |     98.56 us |  1.00 |    0.00 |   7.8125 |        - |        - |    141989 B |        1.00 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | DevIL      | 4096 | Jpeg   |   880,062.1 us | 338,462.94 us | 18,552.30 us |  5.23 |    0.11 |        - |        - |        - |  17304376 B |        0.11 |
// | FreeImage  | 4096 | Jpeg   |   161,316.1 us |  38,785.02 us |  2,125.94 us |  0.96 |    0.01 |        - |        - |        - |  50334976 B |        0.33 |
// | ImageSharp | 4096 | Jpeg   |   116,176.3 us |   6,356.30 us |    348.41 us |  0.69 |    0.00 |        - |        - |        - |   1921187 B |        0.01 |
// | PixCodec   | 4096 | Jpeg   |   168,123.8 us |   3,509.60 us |    192.37 us |  1.00 |    0.00 | 333.3333 | 333.3333 | 333.3333 | 151366277 B |        1.00 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | DevIL      | 4096 | Png    | 1,156,388.6 us | 199,575.69 us | 10,939.42 us |  8.27 |    0.11 |        - |        - |        - |    936072 B |        0.85 |
// | FreeImage  | 4096 | Png    |   635,542.2 us | 118,840.61 us |  6,514.05 us |  4.55 |    0.04 |        - |        - |        - |           - |        0.00 |
// | ImageSharp | 4096 | Png    |   326,781.3 us |  90,694.03 us |  4,971.25 us |  2.34 |    0.05 |        - |        - |        - |   5002144 B |        4.53 |
// | PixCodec   | 4096 | Png    |   139,796.3 us |  15,229.47 us |    834.78 us |  1.00 |    0.00 |        - |        - |        - |   1104416 B |        1.00 |
// |            |      |        |                |               |              |       |         |          |          |          |             |             |
// | FreeImage  | 4096 | Tiff   |   300,030.7 us |  47,385.93 us |  2,597.38 us |  8.67 |    0.09 |        - |        - |        - |      4504 B |       0.001 |
// | PixCodec   | 4096 | Tiff   |    34,605.9 us |   1,847.59 us |    101.27 us |  1.00 |    0.00 | 200.0000 |  66.6667 |        - |   3591192 B |       1.000 |

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
// .NET SDK 10.0.401
//   [Host] : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2 DEBUG
//
// Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3
// LaunchCount=1  WarmupCount=3
//
// | Method     | Size | Format | Mean         | Error        | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated   | Alloc Ratio |
// |----------- |----- |------- |-------------:|-------------:|----------:|------:|--------:|---------:|---------:|---------:|------------:|------------:|
// | DevIL      | 256  | Jpeg   |     912.8 us |    152.79 us |   8.37 us |  2.40 |    0.02 |  61.5234 |  61.5234 |  61.5234 |   243.74 KB |        1.24 |
// | FreeImage  | 256  | Jpeg   |     630.4 us |    372.40 us |  20.41 us |  1.66 |    0.05 |  61.5234 |  61.5234 |  61.5234 |   194.09 KB |        0.98 |
// | ImageSharp | 256  | Jpeg   |     891.1 us |     40.64 us |   2.23 us |  2.34 |    0.01 |  61.5234 |  61.5234 |  61.5234 |   325.34 KB |        1.65 |
// | PixCodec   | 256  | Jpeg   |     380.6 us |      3.15 us |   0.17 us |  1.00 |    0.00 |  62.5000 |  62.5000 |  62.5000 |   197.06 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | DevIL      | 256  | Png    |     524.1 us |     52.16 us |   2.86 us |  3.06 |    0.01 |  83.0078 |  83.0078 |  83.0078 |   262.48 KB |        0.58 |
// | FreeImage  | 256  | Png    |     644.9 us |     17.01 us |   0.93 us |  3.76 |    0.01 |  83.0078 |  83.0078 |  83.0078 |   258.21 KB |        0.57 |
// | ImageSharp | 256  | Png    |     315.3 us |     28.31 us |   1.55 us |  1.84 |    0.01 |  83.0078 |  83.0078 |  83.0078 |   378.76 KB |        0.83 |
// | PixCodec   | 256  | Png    |     171.3 us |      8.84 us |   0.48 us |  1.00 |    0.00 | 133.3008 | 133.3008 | 133.3008 |   455.55 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | FreeImage  | 256  | Tiff   |     513.3 us |     87.97 us |   4.82 us |  4.88 |    0.05 |  71.2891 |  71.2891 |  71.2891 |   258.58 KB |        0.98 |
// | PixCodec   | 256  | Tiff   |     105.2 us |      1.96 us |   0.11 us |  1.00 |    0.00 |  60.0586 |  59.6924 |  59.6924 |   263.38 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | DevIL      | 1024 | Jpeg   |  12,942.8 us |    138.12 us |   7.57 us |  2.16 |    0.00 | 296.8750 | 296.8750 | 296.8750 |  3838.39 KB |        1.25 |
// | FreeImage  | 1024 | Jpeg   |   9,243.1 us |    370.77 us |  20.32 us |  1.54 |    0.01 | 250.0000 | 250.0000 | 250.0000 |  3076.92 KB |        1.00 |
// | ImageSharp | 1024 | Jpeg   |  10,845.1 us |    219.77 us |  12.05 us |  1.81 |    0.00 | 265.6250 | 250.0000 | 250.0000 |  3554.32 KB |        1.15 |
// | PixCodec   | 1024 | Jpeg   |   5,998.0 us |    229.37 us |  12.57 us |  1.00 |    0.00 | 976.5625 | 976.5625 | 976.5625 |  3081.43 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | DevIL      | 1024 | Png    |   8,219.6 us |    580.34 us |  31.81 us |  7.72 |    0.16 | 312.5000 | 312.5000 | 312.5000 |  4159.73 KB |        0.97 |
// | FreeImage  | 1024 | Png    |  10,531.9 us |    280.00 us |  15.35 us |  9.89 |    0.16 | 312.5000 | 312.5000 | 312.5000 |   4101.7 KB |        0.95 |
// | ImageSharp | 1024 | Png    |   2,706.0 us |    104.79 us |   5.74 us |  2.54 |    0.04 | 359.3750 | 332.0313 | 332.0313 |  4568.49 KB |        1.06 |
// | PixCodec   | 1024 | Png    |   1,064.7 us |    331.45 us |  18.17 us |  1.00 |    0.00 | 498.0469 | 496.0938 | 496.0938 |   4297.3 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | FreeImage  | 1024 | Tiff   |   6,731.5 us |    748.59 us |  41.03 us | 10.91 |    0.73 | 234.3750 | 234.3750 | 234.3750 |  4100.68 KB |        1.00 |
// | PixCodec   | 1024 | Tiff   |     618.9 us |    703.95 us |  38.59 us |  1.00 |    0.00 | 157.2266 | 155.2734 | 155.2734 |  4119.05 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | DevIL      | 4096 | Jpeg   | 202,358.0 us |  3,870.12 us | 212.13 us |  2.42 |    0.01 |        - |        - |        - | 61291.39 KB |        1.25 |
// | FreeImage  | 4096 | Jpeg   | 143,719.8 us |  8,526.81 us | 467.38 us |  1.72 |    0.01 |        - |        - |        - | 49155.03 KB |        1.00 |
// | ImageSharp | 4096 | Jpeg   | 164,764.6 us |    574.22 us |  31.47 us |  1.97 |    0.00 |        - |        - |        - | 51027.93 KB |        1.04 |
// | PixCodec   | 4096 | Jpeg   |  83,697.1 us |  2,711.78 us | 148.64 us |  1.00 |    0.00 | 571.4286 | 571.4286 | 571.4286 | 49160.17 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | DevIL      | 4096 | Png    | 132,822.8 us |  3,576.07 us | 196.02 us |  8.26 |    0.02 |        - |        - |        - | 66457.38 KB |        1.01 |
// | FreeImage  | 4096 | Png    | 161,912.0 us |  3,946.00 us | 216.29 us | 10.07 |    0.02 |        - |        - |        - |  65539.1 KB |        1.00 |
// | ImageSharp | 4096 | Png    |  39,349.1 us |  2,232.44 us | 122.37 us |  2.45 |    0.01 | 384.6154 | 307.6923 | 307.6923 | 67402.96 KB |        1.03 |
// | PixCodec   | 4096 | Png    |  16,082.4 us |    304.79 us |  16.71 us |  1.00 |    0.00 | 468.7500 | 468.7500 | 468.7500 | 65737.31 KB |        1.00 |
// |            |      |        |              |              |           |       |         |          |          |          |             |             |
// | FreeImage  | 4096 | Tiff   | 105,132.2 us | 10,288.88 us | 563.97 us | 15.14 |    0.08 |        - |        - |        - | 65538.78 KB |        1.00 |
// | PixCodec   | 4096 | Tiff   |   6,945.8 us |     77.68 us |   4.26 us |  1.00 |    0.00 | 328.1250 | 328.1250 | 328.1250 | 65584.44 KB |        1.00 |

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