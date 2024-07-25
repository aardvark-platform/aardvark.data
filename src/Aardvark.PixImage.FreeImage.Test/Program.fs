namespace Tests

// BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1706 (21H2)
// Intel Core i5-4690K CPU 3.50GHz (Haswell), 1 CPU, 4 logical and 4 physical cores
// .NET SDK=6.0.300
//   [Host] : .NET 6.0.5 (6.0.522.21309), X64 RyuJIT DEBUG
//
// Toolchain=InProcessEmitToolchain  InvocationCount=1  UnrollFactor=1
//
// |              Method | Size | Format |       Mean |     Error |    StdDev |     Median | Allocated |
// |-------------------- |----- |------- |-----------:|----------:|----------:|-----------:|----------:|
// |      'Load (DevIL)' |  256 |   Jpeg |   2.201 ms | 0.0601 ms | 0.1735 ms |   2.208 ms |    208 KB |
// |  'Load (FreeImage)' |  256 |   Jpeg |   2.038 ms | 0.0587 ms | 0.1693 ms |   2.066 ms |    196 KB |
// | 'Load (ImageSharp)' |  256 |   Jpeg |   2.395 ms | 0.1060 ms | 0.3109 ms |   2.497 ms |    350 KB |
// |      'Save (DevIL)' |  256 |   Jpeg |   3.520 ms | 0.0689 ms | 0.1133 ms |   3.475 ms |      3 KB |
// |  'Save (FreeImage)' |  256 |   Jpeg |   2.863 ms | 0.0572 ms | 0.1383 ms |   2.852 ms |    195 KB |
// | 'Save (ImageSharp)' |  256 |   Jpeg |   1.856 ms | 0.0368 ms | 0.0551 ms |   1.861 ms |    153 KB |
// |      'Load (DevIL)' |  256 |    Png |   1.805 ms | 0.0323 ms | 0.0252 ms |   1.803 ms |    272 KB |
// |  'Load (FreeImage)' |  256 |    Png |   1.633 ms | 0.0305 ms | 0.0397 ms |   1.619 ms |    259 KB |
// | 'Load (ImageSharp)' |  256 |    Png |   2.452 ms | 0.0490 ms | 0.1165 ms |   2.410 ms |    403 KB |
// |      'Save (DevIL)' |  256 |    Png |   4.176 ms | 0.0824 ms | 0.0731 ms |   4.158 ms |      3 KB |
// |  'Save (FreeImage)' |  256 |    Png |   3.447 ms | 0.0561 ms | 0.0497 ms |   3.430 ms |      3 KB |
// | 'Save (ImageSharp)' |  256 |    Png |   2.645 ms | 0.0345 ms | 0.0306 ms |   2.633 ms |    170 KB |
// |      'Load (DevIL)' |  256 |   Tiff |   1.617 ms | 0.0645 ms | 0.1882 ms |   1.640 ms |    208 KB |
// |  'Load (FreeImage)' |  256 |   Tiff |   2.262 ms | 0.0555 ms | 0.1637 ms |   2.262 ms |    195 KB |
// | 'Load (ImageSharp)' |  256 |   Tiff |   1.678 ms | 0.0377 ms | 0.1101 ms |   1.681 ms |    418 KB |
// |      'Save (DevIL)' |  256 |   Tiff |   5.396 ms | 0.0963 ms | 0.2232 ms |   5.343 ms |      3 KB |
// |  'Save (FreeImage)' |  256 |   Tiff |   3.201 ms | 0.0634 ms | 0.1160 ms |   3.192 ms |      3 KB |
// | 'Save (ImageSharp)' |  256 |   Tiff |   1.518 ms | 0.0299 ms | 0.0483 ms |   1.526 ms |    153 KB |
// |      'Load (DevIL)' | 1024 |   Jpeg |  14.730 ms | 0.2422 ms | 0.2266 ms |  14.800 ms |  3,088 KB |
// |  'Load (FreeImage)' | 1024 |   Jpeg |  12.761 ms | 0.1611 ms | 0.1428 ms |  12.755 ms |  3,075 KB |
// | 'Load (ImageSharp)' | 1024 |   Jpeg |  13.610 ms | 0.0884 ms | 0.0827 ms |  13.594 ms |  3,638 KB |
// |      'Save (DevIL)' | 1024 |   Jpeg |  37.228 ms | 0.3715 ms | 0.3475 ms |  37.150 ms |      3 KB |
// |  'Save (FreeImage)' | 1024 |   Jpeg |  33.018 ms | 0.0932 ms | 0.0728 ms |  33.028 ms |  3,075 KB |
// | 'Save (ImageSharp)' | 1024 |   Jpeg |  14.019 ms | 0.1764 ms | 0.1650 ms |  13.969 ms |    561 KB |
// |      'Load (DevIL)' | 1024 |    Png |  26.393 ms | 0.1262 ms | 0.1054 ms |  26.377 ms |  4,112 KB |
// |  'Load (FreeImage)' | 1024 |    Png |  23.563 ms | 0.1582 ms | 0.1402 ms |  23.539 ms |  4,099 KB |
// | 'Load (ImageSharp)' | 1024 |    Png |  27.400 ms | 0.2274 ms | 0.2127 ms |  27.404 ms |  4,651 KB |
// |      'Save (DevIL)' | 1024 |    Png |  60.032 ms | 0.4377 ms | 0.4094 ms |  59.953 ms |      3 KB |
// |  'Save (FreeImage)' | 1024 |    Png |  49.464 ms | 0.2245 ms | 0.1874 ms |  49.480 ms |      3 KB |
// | 'Save (ImageSharp)' | 1024 |    Png |  36.670 ms | 0.2387 ms | 0.2233 ms |  36.619 ms |    748 KB |
// |      'Load (DevIL)' | 1024 |   Tiff |   5.711 ms | 0.1111 ms | 0.1520 ms |   5.716 ms |  3,088 KB |
// |  'Load (FreeImage)' | 1024 |   Tiff |   8.019 ms | 0.1594 ms | 0.2749 ms |   7.944 ms |  3,075 KB |
// | 'Load (ImageSharp)' | 1024 |   Tiff |   7.088 ms | 0.1387 ms | 0.2570 ms |   7.057 ms |  4,697 KB |
// |      'Save (DevIL)' | 1024 |   Tiff |  30.678 ms | 0.3907 ms | 0.3655 ms |  30.674 ms |      3 KB |
// |  'Save (FreeImage)' | 1024 |   Tiff |  27.702 ms | 0.3033 ms | 0.2837 ms |  27.660 ms |      3 KB |
// | 'Save (ImageSharp)' | 1024 |   Tiff |   8.472 ms | 0.1686 ms | 0.2471 ms |   8.415 ms |    637 KB |
// |      'Load (DevIL)' | 2048 |   Jpeg |  53.420 ms | 0.3593 ms | 0.3360 ms |  53.313 ms | 12,304 KB |
// |  'Load (FreeImage)' | 2048 |   Jpeg |  46.601 ms | 0.2580 ms | 0.2287 ms |  46.582 ms | 12,291 KB |
// | 'Load (ImageSharp)' | 2048 |   Jpeg |  57.754 ms | 0.3152 ms | 0.2948 ms |  57.755 ms | 13,398 KB |
// |      'Save (DevIL)' | 2048 |   Jpeg | 144.852 ms | 1.4817 ms | 1.2373 ms | 144.482 ms |      3 KB |
// |  'Save (FreeImage)' | 2048 |   Jpeg | 130.007 ms | 0.6692 ms | 0.6260 ms | 129.887 ms | 12,291 KB |
// | 'Save (ImageSharp)' | 2048 |   Jpeg |  57.792 ms | 0.5655 ms | 0.5289 ms |  57.976 ms |  1,106 KB |
// |      'Load (DevIL)' | 2048 |    Png | 105.476 ms | 0.5612 ms | 0.4975 ms | 105.357 ms | 16,400 KB |
// |  'Load (FreeImage)' | 2048 |    Png |  91.624 ms | 0.1998 ms | 0.1869 ms |  91.607 ms | 16,387 KB |
// | 'Load (ImageSharp)' | 2048 |    Png | 101.142 ms | 0.2562 ms | 0.2397 ms | 101.141 ms | 17,484 KB |
// |      'Save (DevIL)' | 2048 |    Png | 233.696 ms | 0.5248 ms | 0.4383 ms | 233.527 ms |      3 KB |
// |  'Save (FreeImage)' | 2048 |    Png | 192.424 ms | 0.3378 ms | 0.2995 ms | 192.311 ms |      4 KB |
// | 'Save (ImageSharp)' | 2048 |    Png | 133.945 ms | 0.7097 ms | 0.6638 ms | 133.924 ms |  1,859 KB |
// |      'Load (DevIL)' | 2048 |   Tiff |  20.119 ms | 0.2308 ms | 0.2046 ms |  20.125 ms | 12,304 KB |
// |  'Load (FreeImage)' | 2048 |   Tiff |  26.470 ms | 0.1048 ms | 0.0875 ms |  26.462 ms | 12,291 KB |
// | 'Load (ImageSharp)' | 2048 |   Tiff |  23.498 ms | 0.0672 ms | 0.0596 ms |  23.482 ms | 17,628 KB |
// |      'Save (DevIL)' | 2048 |   Tiff | 107.692 ms | 1.1649 ms | 1.0327 ms | 107.409 ms |      3 KB |
// |  'Save (FreeImage)' | 2048 |   Tiff | 107.051 ms | 1.3096 ms | 1.2250 ms | 106.966 ms |      3 KB |
// | 'Save (ImageSharp)' | 2048 |   Tiff |  30.057 ms | 0.2856 ms | 0.2532 ms |  30.030 ms |  1,422 KB |
// |      'Load (DevIL)' | 4096 |   Jpeg | 213.389 ms | 0.8850 ms | 0.7846 ms | 213.305 ms | 49,168 KB |
// |  'Load (FreeImage)' | 4096 |   Jpeg | 195.862 ms | 0.6260 ms | 0.5228 ms | 195.948 ms | 49,155 KB |
// | 'Load (ImageSharp)' | 4096 |   Jpeg | 194.918 ms | 1.4201 ms | 1.3283 ms | 194.854 ms | 51,351 KB |
// |      'Save (DevIL)' | 4096 |   Jpeg | 584.503 ms | 7.6716 ms | 7.1760 ms | 584.103 ms |      3 KB |
// |  'Save (FreeImage)' | 4096 |   Jpeg | 524.795 ms | 4.5830 ms | 4.2869 ms | 523.926 ms | 49,156 KB |
// | 'Save (ImageSharp)' | 4096 |   Jpeg | 223.632 ms | 1.6468 ms | 1.4598 ms | 223.265 ms |  2,195 KB |
// |      'Load (DevIL)' | 4096 |    Png | 420.787 ms | 1.7051 ms | 1.5950 ms | 420.523 ms | 65,552 KB |
// |  'Load (FreeImage)' | 4096 |    Png | 380.801 ms | 0.8818 ms | 0.7817 ms | 380.714 ms | 65,539 KB |
// | 'Load (ImageSharp)' | 4096 |    Png | 387.356 ms | 0.7522 ms | 0.7036 ms | 387.442 ms | 67,726 KB |
// |      'Save (DevIL)' | 4096 |    Png | 920.468 ms | 1.4020 ms | 1.3115 ms | 920.289 ms |      3 KB |
// |  'Save (FreeImage)' | 4096 |    Png | 761.950 ms | 1.6772 ms | 1.4868 ms | 762.160 ms |      3 KB |
// | 'Save (ImageSharp)' | 4096 |    Png | 512.992 ms | 4.4328 ms | 4.1465 ms | 514.117 ms |  5,208 KB |
// |      'Load (DevIL)' | 4096 |   Tiff |  63.992 ms | 0.3387 ms | 0.2828 ms |  63.908 ms | 49,168 KB |
// |  'Load (FreeImage)' | 4096 |   Tiff | 100.322 ms | 0.9950 ms | 0.8308 ms |  99.971 ms | 49,155 KB |
// | 'Load (ImageSharp)' | 4096 |   Tiff |  82.975 ms | 1.4431 ms | 1.2792 ms |  82.396 ms | 68,002 KB |
// |      'Save (DevIL)' | 4096 |   Tiff | 509.067 ms | 8.5501 ms | 7.5794 ms | 509.399 ms |      3 KB |
// |  'Save (FreeImage)' | 4096 |   Tiff | 401.969 ms | 6.7875 ms | 6.3491 ms | 403.147 ms |      3 KB |
// | 'Save (ImageSharp)' | 4096 |   Tiff |  99.599 ms | 1.6361 ms | 1.4503 ms |  99.894 ms |  2,831 KB |

module Program =

    open BenchmarkDotNet.Running
    open BenchmarkDotNet.Configs
    open BenchmarkDotNet.Jobs
    open BenchmarkDotNet.Toolchains

    [<EntryPoint>]
    let main argv =

        //TiffTests.``[PixImage] Load TIFFs``()
        //exit 0

        let cfg =
            let job = Job.Default.WithToolchain(InProcess.Emit.InProcessEmitToolchain.Instance)
            ManualConfig.Create(DefaultConfig.Instance).WithOptions(ConfigOptions.DisableOptimizationsValidator).AddJob(job)

        BenchmarkSwitcher.FromAssembly(typeof<LoaderBenchmark>.Assembly).Run(argv, cfg) |> ignore
        0