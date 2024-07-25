using System.IO;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FreeImageAPI;
using FreeImageAPI.IO;

namespace Benchmarking.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [MemoryDiagnoser]
    public class StreamWriteBenchmark
    {
        private FIBITMAP bmp;
        private Stream stream;

        [GlobalSetup]
        public void Setup()
        {
            bmp = FreeImage.Allocate(1000, 1000, 24);
            stream = new MemoryStream(3000054);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            FreeImage.Unload(bmp);
            stream.Dispose();
        }

        [Benchmark(Baseline = true)]
        public void WriteLegacy()
        {
            FreeImage.IO = FreeImageStreamIO.IO;
            Exec();
        }

        [Benchmark]
        public void WriteSpan()
        {
            FreeImage.IO = SpanStreamIO.IO;
            Exec();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Exec()
        {
            stream.Seek(0, SeekOrigin.Begin);
            FreeImage.SaveToStream(bmp, stream, FREE_IMAGE_FORMAT.FIF_BMP);
        }
    }
}
